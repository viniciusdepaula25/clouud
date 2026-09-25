using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Plataformas de ativação (Steam, Epic Games, Ubisoft Connect...).</summary>
    [Authorize(Roles = "Admin")]
    public class PlataformaController : AdminController
    {
        private readonly BancoDados bancoDados;

        public PlataformaController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var plataformas = bancoDados.Plataformas
                .Include(p => p.Produtos)
                .OrderBy(p => p.Nome)
                .ToList();
            return View(plataformas);
        }

        [HttpGet]
        public IActionResult Inclui()
        {
            return View(new Plataforma());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(Plataforma plataforma)
        {
            ValidarNome(plataforma);
            if (!ModelState.IsValid)
            {
                return View(plataforma);
            }

            plataforma.Nome = plataforma.Nome.Trim();
            plataforma.Slug = SlugHelper.Gerar(plataforma.Nome);
            bancoDados.Plataformas.Add(plataforma);
            bancoDados.SaveChanges();

            TempData["Mensagem"] = $"Plataforma \"{plataforma.Nome}\" cadastrada.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var plataforma = bancoDados.Plataformas.FirstOrDefault(p => p.Id == id);
            return plataforma == null ? NotFound() : View(plataforma);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(Plataforma dados)
        {
            var plataforma = bancoDados.Plataformas.FirstOrDefault(p => p.Id == dados.Id);
            if (plataforma == null)
            {
                return NotFound();
            }

            ValidarNome(dados);
            if (!ModelState.IsValid)
            {
                return View(dados);
            }

            plataforma.Nome = dados.Nome.Trim();
            plataforma.Slug = SlugHelper.Gerar(plataforma.Nome);
            plataforma.InstrucoesAtivacao = dados.InstrucoesAtivacao?.Trim();
            plataforma.Ativa = dados.Ativa;
            bancoDados.SaveChanges();

            TempData["Mensagem"] = $"Plataforma \"{plataforma.Nome}\" alterada.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var plataforma = bancoDados.Plataformas.Include(p => p.Produtos).FirstOrDefault(p => p.Id == id);
            return plataforma == null ? NotFound() : View(plataforma);
        }

        [HttpPost, ActionName("Exclui")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmaExclusao(int id)
        {
            var plataforma = bancoDados.Plataformas.Include(p => p.Produtos).FirstOrDefault(p => p.Id == id);
            if (plataforma == null)
            {
                return NotFound();
            }

            if (plataforma.Produtos.Count > 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Esta plataforma tem produtos cadastrados e não pode ser excluída. Desmarque \"Ativa\" para escondê-la da loja.");
                return View(plataforma);
            }

            bancoDados.Plataformas.Remove(plataforma);
            bancoDados.SaveChanges();

            TempData["Mensagem"] = $"Plataforma \"{plataforma.Nome}\" excluída.";
            return RedirectToAction("Index");
        }

        private void ValidarNome(Plataforma plataforma)
        {
            var slug = SlugHelper.Gerar(plataforma.Nome ?? string.Empty);
            if (!string.IsNullOrEmpty(slug) && bancoDados.Plataformas.Any(p => p.Slug == slug && p.Id != plataforma.Id))
            {
                ModelState.AddModelError(nameof(plataforma.Nome), "Já existe uma plataforma com este nome");
            }
        }
    }
}
