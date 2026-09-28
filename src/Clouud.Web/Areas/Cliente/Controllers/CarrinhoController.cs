using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Clouud.Web.Infraestrutura;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    [Authorize(Roles = "Admin,Cliente")]
    public class CarrinhoController : ClienteLoginController
    {
        /// <summary>Cookie que guarda o cupom digitado até o cliente finalizar a compra.</summary>
        private const string CookieCupom = "clouud_cupom";

        private readonly CarrinhoService carrinho;
        private readonly PedidoService pedidos;
        private readonly CupomService cupons;

        public CarrinhoController(IWebHostEnvironment webHostEnvironment, CarrinhoService carrinho, PedidoService pedidos,
            CupomService cupons) : base(webHostEnvironment)
        {
            this.carrinho = carrinho;
            this.pedidos = pedidos;
            this.cupons = cupons;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var modelo = carrinho.Montar(UsuarioId);
            var codigo = Request.Cookies[CookieCupom];
            if (!string.IsNullOrEmpty(codigo) && modelo.Itens.Count > 0)
            {
                // O cupom é conferido de novo a cada vez: o carrinho pode ter mudado desde que foi aplicado
                var resultado = cupons.Validar(codigo, UsuarioId, modelo.Subtotal);
                modelo.CodigoCupom = resultado.Cupom?.Codigo ?? CupomService.Normalizar(codigo);
                modelo.DescricaoCupom = resultado.Cupom?.DescreverDesconto();
                modelo.Desconto = resultado.Desconto;
                modelo.ErroCupom = resultado.Erro;
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(LimitesDeUso.Compras)]
        public IActionResult AplicarCupom(string? codigo)
        {
            var subtotal = carrinho.Montar(UsuarioId).Subtotal;
            var resultado = cupons.Validar(codigo, UsuarioId, subtotal);
            if (resultado.Cupom == null)
            {
                TempData["Erro"] = resultado.Erro;
                return RedirectToAction("Index");
            }

            // Cupom existe: fica guardado mesmo que ainda não valha (ex.: falta chegar ao pedido mínimo)
            Response.Cookies.Append(CookieCupom, resultado.Cupom.Codigo, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            if (resultado.Valido)
            {
                TempData["Mensagem"] = $"Cupom {resultado.Cupom.Codigo} aplicado: {resultado.Cupom.DescreverDesconto()} de desconto.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverCupom()
        {
            Response.Cookies.Delete(CookieCupom);
            TempData["Mensagem"] = "Cupom removido.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Adicionar(int produtoId, string? voltarPara)
        {
            var erro = carrinho.Adicionar(UsuarioId, produtoId);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                // Volta para a vitrine (com os filtros) para o cliente ver o aviso
                return Url.IsLocalUrl(voltarPara) ? LocalRedirect(voltarPara) : RedirectToAction("Index", "Home");
            }

            TempData["Mensagem"] = "Produto adicionado ao carrinho.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Atualizar(int produtoId, int quantidade)
        {
            var aviso = carrinho.Atualizar(UsuarioId, produtoId, quantidade);
            if (aviso != null)
            {
                TempData["Erro"] = aviso;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remover(int produtoId)
        {
            carrinho.Remover(UsuarioId, produtoId);
            TempData["Mensagem"] = "Produto removido do carrinho.";
            return RedirectToAction("Index");
        }

        /// <summary>Fecha o pedido: reserva as chaves e leva para o pagamento.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(LimitesDeUso.Compras)]
        public IActionResult Finalizar()
        {
            var (pedido, erro) = pedidos.CriarDoCarrinho(UsuarioId, Request.Cookies[CookieCupom]);
            if (pedido == null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Index");
            }
            Response.Cookies.Delete(CookieCupom);
            return RedirectToAction("Pagar", "Pedidos", new { id = pedido.Id });
        }
    }
}
