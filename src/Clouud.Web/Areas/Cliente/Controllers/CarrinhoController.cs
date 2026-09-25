using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    [Authorize(Roles = "Admin,Cliente")]
    public class CarrinhoController : ClienteLoginController
    {
        private readonly CarrinhoService carrinho;
        private readonly PedidoService pedidos;

        public CarrinhoController(IWebHostEnvironment webHostEnvironment, CarrinhoService carrinho, PedidoService pedidos)
            : base(webHostEnvironment)
        {
            this.carrinho = carrinho;
            this.pedidos = pedidos;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(carrinho.Montar(UsuarioId));
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
        public IActionResult Finalizar()
        {
            var (pedido, erro) = pedidos.CriarDoCarrinho(UsuarioId);
            if (pedido == null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Index");
            }
            return RedirectToAction("Pagar", "Pedidos", new { id = pedido.Id });
        }
    }
}
