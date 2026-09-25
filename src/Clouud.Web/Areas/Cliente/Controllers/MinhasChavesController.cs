using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    /// <summary>Todas as chaves compradas pelo cliente, com o passo a passo de ativação.</summary>
    [Authorize(Roles = "Admin,Cliente")]
    public class MinhasChavesController : ClienteLoginController
    {
        private readonly BancoDados bancoDados;

        public MinhasChavesController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var chaves = bancoDados.Chaves
                .Where(c => c.Status == StatusChave.Vendida
                            && c.PedidoItem!.Pedido.UsuarioId == UsuarioId
                            && c.PedidoItem.Pedido.Status == StatusPedido.Pago)
                .Include(c => c.Produto).ThenInclude(p => p.Jogo)
                .Include(c => c.Produto).ThenInclude(p => p.Plataforma)
                .Include(c => c.PedidoItem)
                .OrderByDescending(c => c.VendidaEm).ThenBy(c => c.Id)
                .ToList();
            return View(chaves);
        }
    }
}
