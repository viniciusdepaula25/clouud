using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Categorias (gêneros) dos jogos. Tudo numa tela só: listar, incluir, renomear e excluir.</summary>
    [Authorize(Roles = "Admin")]
    public class CategoriaController : AdminController
    {
        private readonly BancoDados bancoDados;

        public CategoriaController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var categorias = bancoDados.Categorias.Include(c => c.Jogos).OrderBy(c => c.Nome).ToList();
            return View(categorias);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(string nome)
        {
            var erro = Validar(nome, 0);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Index");
            }

            bancoDados.Categorias.Add(new Categoria { Nome = nome.Trim(), Slug = SlugHelper.Gerar(nome) });
            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Categoria \"{nome.Trim()}\" cadastrada.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(int id, string nome)
        {
            var categoria = bancoDados.Categorias.FirstOrDefault(c => c.Id == id);
            if (categoria == null)
            {
                return NotFound();
            }

            var erro = Validar(nome, id);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Index");
            }

            categoria.Nome = nome.Trim();
            categoria.Slug = SlugHelper.Gerar(nome);
            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Categoria renomeada para \"{categoria.Nome}\".";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Exclui(int id)
        {
            var categoria = bancoDados.Categorias.FirstOrDefault(c => c.Id == id);
            if (categoria != null)
            {
                // Os vínculos com os jogos (JogoCategorias) são apagados junto; os jogos continuam
                bancoDados.Categorias.Remove(categoria);
                bancoDados.SaveChanges();
                TempData["Mensagem"] = $"Categoria \"{categoria.Nome}\" excluída.";
            }
            return RedirectToAction("Index");
        }

        private string? Validar(string? nome, int id)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "Informe o nome da categoria";
            }
            if (nome.Trim().Length > 60)
            {
                return "O nome pode ter no máximo 60 caracteres";
            }
            var slug = SlugHelper.Gerar(nome);
            if (bancoDados.Categorias.Any(c => c.Slug == slug && c.Id != id))
            {
                return $"A categoria \"{nome.Trim()}\" já existe";
            }
            return null;
        }
    }
}
