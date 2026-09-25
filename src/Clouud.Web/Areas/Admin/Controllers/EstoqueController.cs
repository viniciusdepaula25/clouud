using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Estoque de chaves: visão geral, chaves de cada produto e importação.</summary>
    [Authorize(Roles = "Admin")]
    public class EstoqueController : AdminController
    {
        private readonly BancoDados bancoDados;
        private readonly EstoqueService estoque;

        public EstoqueController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, EstoqueService estoque)
            : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.estoque = estoque;
        }

        [HttpGet]
        public IActionResult Index(string? busca)
        {
            var consulta = bancoDados.Produtos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                consulta = consulta.Where(p => EF.Functions.ILike(p.Jogo.Titulo, $"%{busca.Trim()}%"));
            }

            ViewData["Busca"] = busca;
            var produtos = ResumoDosProdutos(consulta)
                .OrderBy(p => p.Jogo).ThenBy(p => p.Plataforma).ThenBy(p => p.Edicao)
                .ToList();
            return View(produtos);
        }

        /// <summary>Chaves de um produto (id do produto), com filtro opcional por situação.</summary>
        [HttpGet]
        public IActionResult Chaves(int id, StatusChave? status)
        {
            var produto = ResumoDosProdutos(bancoDados.Produtos.Where(p => p.Id == id)).FirstOrDefault();
            if (produto == null)
            {
                return NotFound();
            }

            var consulta = bancoDados.Chaves.Where(c => c.ProdutoId == id);
            if (status.HasValue)
            {
                consulta = consulta.Where(c => c.Status == status);
            }

            return View(new ChavesProdutoViewModel
            {
                Produto = produto,
                Filtro = status,
                Total = consulta.Count(),
                Chaves = consulta
                    .Include(c => c.PedidoItem)
                    .OrderByDescending(c => c.AdicionadaEm).ThenByDescending(c => c.Id)
                    .Take(ChavesProdutoViewModel.LimiteListagem)
                    .ToList()
            });
        }

        [HttpGet]
        public IActionResult Importar(int id)
        {
            var produto = ResumoDosProdutos(bancoDados.Produtos.Where(p => p.Id == id)).FirstOrDefault();
            return produto == null ? NotFound() : View(new ImportarChavesViewModel { Produto = produto });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueLengthLimit = 2 * 1024 * 1024)]
        public IActionResult Importar(int id, ImportarChavesViewModel form)
        {
            var produto = ResumoDosProdutos(bancoDados.Produtos.Where(p => p.Id == id)).FirstOrDefault();
            if (produto == null)
            {
                return NotFound();
            }

            var resultado = estoque.Importar(id, form.Codigos);
            if (resultado.Erro != null || resultado.Adicionadas == 0)
            {
                // Nada entrou: mantém o texto na tela para o admin corrigir
                ModelState.AddModelError(string.Empty, resultado.Erro ?? "Nenhuma chave nova foi importada.");
                ViewData["Resultado"] = resultado;
                form.Produto = produto;
                return View(form);
            }

            TempData["Mensagem"] = resultado.Adicionadas == 1
                ? "1 chave importada."
                : $"{resultado.Adicionadas} chaves importadas.";
            var ignoradas = resultado.RepetidasNoTexto.Count + resultado.JaCadastradas.Count + resultado.Invalidas.Count;
            if (ignoradas > 0)
            {
                TempData["Aviso"] = DescreverIgnoradas(resultado);
            }
            return RedirectToAction("Chaves", new { id });
        }

        /// <summary>Tira uma chave disponível da venda.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inativar(int id, StatusChave? filtro)
        {
            return MudarStatus(id, filtro, StatusChave.Disponivel, StatusChave.Inativa, "Chave inativada.");
        }

        /// <summary>Devolve uma chave inativa ao estoque.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id, StatusChave? filtro)
        {
            // Chave de pedido reembolsado não volta: o cliente já viu o código
            var chave = bancoDados.Chaves.FirstOrDefault(c => c.Id == id);
            if (chave != null && !chave.NuncaFoiVendida)
            {
                TempData["Erro"] = "Esta chave já foi entregue a um cliente e não pode voltar ao estoque.";
                return RedirectToAction("Chaves", new { id = chave.ProdutoId, status = filtro });
            }
            return MudarStatus(id, filtro, StatusChave.Inativa, StatusChave.Disponivel, "Chave devolvida ao estoque.");
        }

        /// <summary>Apaga uma chave que nunca foi vendida (disponível ou inativa).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int id, StatusChave? filtro)
        {
            var chave = bancoDados.Chaves.FirstOrDefault(c => c.Id == id);
            if (chave == null)
            {
                return NotFound();
            }

            if (chave.Status is StatusChave.Disponivel or StatusChave.Inativa && chave.NuncaFoiVendida)
            {
                bancoDados.Chaves.Remove(chave);
                bancoDados.SaveChanges();
                TempData["Mensagem"] = "Chave excluída.";
            }
            else
            {
                TempData["Erro"] = "Chaves que já foram para um pedido não podem ser excluídas.";
            }
            return RedirectToAction("Chaves", new { id = chave.ProdutoId, status = filtro });
        }

        private IActionResult MudarStatus(int id, StatusChave? filtro, StatusChave de, StatusChave para, string mensagem)
        {
            var chave = bancoDados.Chaves.FirstOrDefault(c => c.Id == id);
            if (chave == null)
            {
                return NotFound();
            }

            if (chave.Status == de)
            {
                chave.Status = para;
                bancoDados.SaveChanges();
                TempData["Mensagem"] = mensagem;
            }
            else
            {
                TempData["Erro"] = "A situação desta chave mudou. Confira a lista e tente de novo.";
            }
            return RedirectToAction("Chaves", new { id = chave.ProdutoId, status = filtro });
        }

        private static IQueryable<EstoqueProdutoViewModel> ResumoDosProdutos(IQueryable<Produto> produtos)
        {
            return produtos.Select(p => new EstoqueProdutoViewModel
            {
                ProdutoId = p.Id,
                Jogo = p.Jogo.Titulo,
                Plataforma = p.Plataforma.Nome,
                Edicao = p.Edicao,
                Ativo = p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa,
                Disponiveis = p.Chaves.Count(c => c.Status == StatusChave.Disponivel),
                Reservadas = p.Chaves.Count(c => c.Status == StatusChave.Reservada),
                Vendidas = p.Chaves.Count(c => c.Status == StatusChave.Vendida),
                Inativas = p.Chaves.Count(c => c.Status == StatusChave.Inativa)
            });
        }

        private static string DescreverIgnoradas(ResultadoImportacao resultado)
        {
            var partes = new List<string>();
            if (resultado.JaCadastradas.Count > 0)
            {
                partes.Add($"{resultado.JaCadastradas.Count} já estavam cadastradas");
            }
            if (resultado.RepetidasNoTexto.Count > 0)
            {
                partes.Add($"{resultado.RepetidasNoTexto.Count} estavam repetidas no texto");
            }
            if (resultado.Invalidas.Count > 0)
            {
                partes.Add($"{resultado.Invalidas.Count} passavam de {Chave.TamanhoMaximoCodigo} caracteres");
            }
            return "Ignoradas: " + string.Join(", ", partes) + ".";
        }
    }
}
