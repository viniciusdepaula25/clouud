using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    /// <summary>"Meus pedidos": lista, detalhes com as chaves e o pagamento (simulado).</summary>
    [Authorize(Roles = "Admin,Cliente")]
    public class PedidosController : ClienteLoginController
    {
        private readonly BancoDados bancoDados;
        private readonly PedidoService pedidos;

        public PedidosController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, PedidoService pedidos)
            : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.pedidos = pedidos;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var lista = bancoDados.Pedidos
                .Where(p => p.UsuarioId == UsuarioId)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Jogo)
                .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
                .AsSplitQuery()
                .ToList();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            var pedido = BuscarDoUsuario(id);
            return pedido == null ? NotFound() : View(pedido);
        }

        [HttpGet]
        public IActionResult Pagar(int id)
        {
            var pedido = BuscarDoUsuario(id);
            if (pedido == null)
            {
                return NotFound();
            }
            if (pedido.Status != StatusPedido.AguardandoPagamento)
            {
                return RedirectToAction("Detalhes", new { id });
            }
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pagar(int id, MetodoPagamento metodo, bool aprovar)
        {
            var erro = pedidos.Pagar(id, UsuarioId, metodo, aprovar);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Detalhes", new { id });
            }
            if (!aprovar)
            {
                TempData["Erro"] = "Pagamento recusado (simulação). Escolha outra forma de pagamento ou tente de novo.";
                return RedirectToAction("Pagar", new { id });
            }

            TempData["Mensagem"] = "Pagamento aprovado! Suas chaves estão abaixo.";
            return RedirectToAction("Detalhes", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            var erro = pedidos.Cancelar(id, UsuarioId);
            if (erro != null)
            {
                TempData["Erro"] = erro;
            }
            else
            {
                TempData["Mensagem"] = "Pedido cancelado.";
            }
            return RedirectToAction("Detalhes", new { id });
        }

        private Pedido? BuscarDoUsuario(int id)
        {
            return bancoDados.Pedidos
                .Where(p => p.Id == id && p.UsuarioId == UsuarioId)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Jogo)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Plataforma)
                .Include(p => p.Itens).ThenInclude(i => i.Chaves)
                .Include(p => p.Pagamentos)
                .AsSplitQuery()
                .FirstOrDefault();
        }
    }
}
