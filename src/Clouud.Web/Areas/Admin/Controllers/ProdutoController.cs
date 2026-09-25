using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Produtos: jogo + plataforma + edição, com preço e promoção.</summary>
    [Authorize(Roles = "Admin")]
    public class ProdutoController : AdminController
    {
        private readonly BancoDados bancoDados;

        public ProdutoController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index(int? jogoId)
        {
            var consulta = bancoDados.Produtos
                .Include(p => p.Jogo)
                .Include(p => p.Plataforma)
                .AsQueryable();

            if (jogoId.HasValue)
            {
                consulta = consulta.Where(p => p.JogoId == jogoId);
                ViewData["JogoTitulo"] = bancoDados.Jogos.Where(j => j.Id == jogoId).Select(j => j.Titulo).FirstOrDefault();
            }

            ViewData["JogoId"] = jogoId;
            return View(consulta.OrderBy(p => p.Jogo.Titulo).ThenBy(p => p.Plataforma.Nome).ThenBy(p => p.Edicao).ToList());
        }

        [HttpGet]
        public IActionResult Inclui(int? jogoId)
        {
            var form = new ProdutoFormViewModel { JogoId = jogoId };
            PreencherOpcoes(form);
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(ProdutoFormViewModel form)
        {
            ValidarDuplicado(form);
            if (!ModelState.IsValid)
            {
                PreencherOpcoes(form);
                return View(form);
            }

            var produto = new Produto();
            Aplicar(form, produto);
            bancoDados.Produtos.Add(produto);
            bancoDados.SaveChanges();

            TempData["Mensagem"] = "Produto cadastrado.";
            return RedirectToAction("Index", new { jogoId = produto.JogoId });
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var produto = bancoDados.Produtos.FirstOrDefault(p => p.Id == id);
            if (produto == null)
            {
                return NotFound();
            }

            var form = new ProdutoFormViewModel
            {
                Id = produto.Id,
                JogoId = produto.JogoId,
                PlataformaId = produto.PlataformaId,
                Edicao = produto.Edicao,
                Regiao = produto.Regiao,
                Preco = produto.Preco,
                PrecoPromocional = produto.PrecoPromocional,
                PromocaoAte = produto.PromocaoAte,
                Ativo = produto.Ativo
            };
            PreencherOpcoes(form);
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(ProdutoFormViewModel form)
        {
            var produto = bancoDados.Produtos.FirstOrDefault(p => p.Id == form.Id);
            if (produto == null)
            {
                return NotFound();
            }

            ValidarDuplicado(form);
            if (!ModelState.IsValid)
            {
                PreencherOpcoes(form);
                return View(form);
            }

            Aplicar(form, produto);
            bancoDados.SaveChanges();

            TempData["Mensagem"] = "Produto alterado.";
            return RedirectToAction("Index", new { jogoId = produto.JogoId });
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var produto = bancoDados.Produtos
                .Include(p => p.Jogo)
                .Include(p => p.Plataforma)
                .FirstOrDefault(p => p.Id == id);
            return produto == null ? NotFound() : View(produto);
        }

        [HttpPost, ActionName("Exclui")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmaExclusao(int id)
        {
            var produto = bancoDados.Produtos
                .Include(p => p.Jogo)
                .Include(p => p.Plataforma)
                .FirstOrDefault(p => p.Id == id);
            if (produto == null)
            {
                return NotFound();
            }

            bancoDados.Produtos.Remove(produto);
            try
            {
                bancoDados.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Este produto já tem vendas e não pode ser excluído. Desmarque \"Ativo\" para tirá-lo da loja.");
                return View(produto);
            }

            TempData["Mensagem"] = "Produto excluído.";
            return RedirectToAction("Index", new { jogoId = produto.JogoId });
        }

        private static void Aplicar(ProdutoFormViewModel form, Produto produto)
        {
            produto.JogoId = form.JogoId!.Value;
            produto.PlataformaId = form.PlataformaId!.Value;
            produto.Edicao = form.Edicao.Trim();
            produto.Regiao = form.Regiao.Trim();
            produto.Preco = form.Preco!.Value;
            produto.PrecoPromocional = form.PrecoPromocional;
            // sem preço promocional, a data da promoção não faz sentido
            produto.PromocaoAte = form.PrecoPromocional.HasValue ? form.PromocaoAte : null;
            produto.Ativo = form.Ativo;
        }

        /// <summary>O mesmo jogo não pode ter dois produtos na mesma plataforma com a mesma edição.</summary>
        private void ValidarDuplicado(ProdutoFormViewModel form)
        {
            if (form.JogoId is null || form.PlataformaId is null)
            {
                return;
            }

            var edicao = form.Edicao.Trim();
            var existe = bancoDados.Produtos.Any(p => p.Id != form.Id && p.JogoId == form.JogoId
                                                      && p.PlataformaId == form.PlataformaId && p.Edicao == edicao);
            if (existe)
            {
                ModelState.AddModelError(nameof(form.Edicao), "Já existe um produto deste jogo nesta plataforma com esta edição");
            }
        }

        private void PreencherOpcoes(ProdutoFormViewModel form)
        {
            form.OpcoesJogos = bancoDados.Jogos.OrderBy(j => j.Titulo)
                .Select(j => new SelectListItem(j.Titulo, j.Id.ToString()))
                .ToList();
            form.OpcoesPlataformas = bancoDados.Plataformas.OrderBy(p => p.Nome)
                .Select(p => new SelectListItem(p.Ativa ? p.Nome : p.Nome + " (inativa)", p.Id.ToString()))
                .ToList();
        }
    }
}
