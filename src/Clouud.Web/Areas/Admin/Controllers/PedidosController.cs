using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Pedidos de todos os clientes: acompanhar, cancelar e reembolsar.</summary>
    [Authorize(Roles = "Admin")]
    public class PedidosController : AdminController
    {
        private const int LimiteListagem = 200;

        private readonly BancoDados bancoDados;
        private readonly PedidoService pedidos;

        public PedidosController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, PedidoService pedidos)
            : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.pedidos = pedidos;
        }

        [HttpGet]
        public IActionResult Index(StatusPedido? status, string? busca)
        {
            var consulta = bancoDados.Pedidos.AsQueryable();
            if (status.HasValue)
            {
                consulta = consulta.Where(p => p.Status == status);
            }
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().TrimStart('#');
                consulta = int.TryParse(termo, out var numero)
                    ? consulta.Where(p => p.Id == numero)
                    : consulta.Where(p => EF.Functions.ILike(p.Usuario.Email, $"%{termo}%")
                                          || EF.Functions.ILike(p.Usuario.Name, $"%{termo}%"));
            }

            ViewData["Status"] = status;
            ViewData["Busca"] = busca;
            ViewData["Total"] = consulta.Count();
            ViewData["Resumo"] = bancoDados.Pedidos
                .GroupBy(p => p.Status)
                .Select(g => new { g.Key, Quantidade = g.Count(), Valor = g.Sum(p => p.Total) })
                .ToDictionary(x => x.Key, x => (x.Quantidade, x.Valor));

            var lista = consulta
                .Include(p => p.Usuario)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Jogo)
                .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
                .Take(LimiteListagem)
                .AsSplitQuery()
                .ToList();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            var pedido = bancoDados.Pedidos
                .Where(p => p.Id == id)
                .Include(p => p.Usuario)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Jogo)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Plataforma)
                .Include(p => p.Itens).ThenInclude(i => i.Chaves)
                .Include(p => p.Pagamentos)
                .AsSplitQuery()
                .FirstOrDefault();
            return pedido == null ? NotFound() : View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            var erro = pedidos.Cancelar(id, null);
            TempData[erro == null ? "Mensagem" : "Erro"] = erro ?? "Pedido cancelado. As chaves reservadas voltaram ao estoque.";
            return RedirectToAction("Detalhes", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reembolsar(int id)
        {
            var erro = pedidos.Reembolsar(id);
            TempData[erro == null ? "Mensagem" : "Erro"] = erro ?? "Pedido reembolsado. As chaves foram desativadas.";
            return RedirectToAction("Detalhes", new { id });
        }
    }
}
