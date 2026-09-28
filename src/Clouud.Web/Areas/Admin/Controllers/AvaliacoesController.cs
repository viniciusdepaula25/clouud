using Clouud.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Moderação das avaliações dos clientes.</summary>
    [Authorize(Roles = "Admin")]
    public class AvaliacoesController : AdminController
    {
        private const int LimiteListagem = 200;
        private readonly BancoDados bancoDados;

        public AvaliacoesController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index(int? nota, string? busca)
        {
            var consulta = bancoDados.Avaliacoes.Include(a => a.Usuario).Include(a => a.Jogo).AsQueryable();
            if (nota is >= 1 and <= 5)
            {
                consulta = consulta.Where(a => a.Nota == nota);
            }
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = $"%{busca.Trim()}%";
                consulta = consulta.Where(a => EF.Functions.ILike(a.Jogo.Titulo, termo) || EF.Functions.ILike(a.Usuario.Name, termo)
                                               || EF.Functions.ILike(a.Comentario ?? "", termo));
            }

            ViewData["Nota"] = nota;
            ViewData["Busca"] = busca;
            return View(consulta.OrderByDescending(a => a.AtualizadaEm ?? a.CriadaEm).Take(LimiteListagem).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int usuarioId, int jogoId, int? nota, string? busca)
        {
            var avaliacao = bancoDados.Avaliacoes.FirstOrDefault(a => a.UsuarioId == usuarioId && a.JogoId == jogoId);
            if (avaliacao != null)
            {
                bancoDados.Avaliacoes.Remove(avaliacao);
                bancoDados.SaveChanges();
                TempData["Mensagem"] = "Avaliação excluída.";
            }
            return RedirectToAction("Index", new { nota, busca });
        }
    }
}
