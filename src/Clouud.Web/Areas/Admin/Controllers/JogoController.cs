using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JogoController : AdminController
    {
        private readonly BancoDados bancoDados;

        public JogoController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(ListarJogos(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string? busca)
        {
            ViewData["Busca"] = busca;
            return View(ListarJogos(busca));
        }

        [HttpGet]
        public IActionResult Inclui()
        {
            var form = new JogoFormViewModel();
            PreencherOpcoes(form);
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(JogoFormViewModel form, IFormFile? arquivo)
        {
            if (!ModelState.IsValid)
            {
                PreencherOpcoes(form);
                return View(form);
            }

            var jogo = new Jogo();
            Aplicar(form, jogo);
            if (arquivo != null)
            {
                jogo.Capa = SalvaArquivo(arquivo);
            }

            bancoDados.Jogos.Add(jogo);
            bancoDados.SaveChanges();

            // Próximo passo natural: cadastrar onde e por quanto o jogo é vendido
            TempData["Mensagem"] = $"Jogo \"{jogo.Titulo}\" cadastrado. Agora cadastre um produto (plataforma e preço).";
            return RedirectToAction("Inclui", "Produto", new { jogoId = jogo.Id });
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var jogo = bancoDados.Jogos
                .Include(e => e.Categorias)
                .Include(e => e.Desenvolvedora)
                .Include(e => e.Publicadora)
                .FirstOrDefault(e => e.Id == id);
            if (jogo == null)
            {
                return NotFound();
            }

            var form = new JogoFormViewModel
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                DataLancamento = jogo.DataLancamento,
                ClassificacaoIndicativa = jogo.ClassificacaoIndicativa,
                Desenvolvedora = jogo.Desenvolvedora?.Nome,
                Publicadora = jogo.Publicadora?.Nome,
                CategoriaIds = jogo.Categorias.Select(c => c.Id).ToList(),
                Destaque = jogo.Destaque,
                Ativo = jogo.Ativo,
                CapaAtual = jogo.Capa
            };
            PreencherOpcoes(form);
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(JogoFormViewModel form, IFormFile? arquivo)
        {
            var jogo = bancoDados.Jogos.Include(e => e.Categorias).FirstOrDefault(e => e.Id == form.Id);
            if (jogo == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                form.CapaAtual = jogo.Capa;
                PreencherOpcoes(form);
                return View(form);
            }

            Aplicar(form, jogo);
            if (arquivo != null)
            {
                //troca a capa: exclui o arquivo antigo e salva o novo
                ExcluiArquivo(jogo.Capa);
                jogo.Capa = SalvaArquivo(arquivo);
            }

            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Jogo \"{jogo.Titulo}\" alterado.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Exibe(int id)
        {
            var jogo = BuscarCompleto(id);
            return jogo == null ? NotFound() : View(jogo);
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var jogo = BuscarCompleto(id);
            return jogo == null ? NotFound() : View(jogo);
        }

        [HttpPost, ActionName("Exclui")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmaExclusao(int id)
        {
            var jogo = BuscarCompleto(id);
            if (jogo == null)
            {
                return NotFound();
            }

            // Jogo com produtos ou pedidos não é apagado: o caminho é desativar
            if (jogo.Produtos.Count > 0 || bancoDados.PedidoJogos.Any(e => e.JogoId == id))
            {
                ModelState.AddModelError(string.Empty,
                    "Este jogo tem produtos ou pedidos e não pode ser excluído. Exclua os produtos ou desmarque \"Ativo\" para tirá-lo da loja.");
                return View(jogo);
            }

            bancoDados.Jogos.Remove(jogo);
            bancoDados.SaveChanges();
            ExcluiArquivo(jogo.Capa);

            TempData["Mensagem"] = $"Jogo \"{jogo.Titulo}\" excluído.";
            return RedirectToAction("Index");
        }

        private List<Jogo> ListarJogos(string? busca)
        {
            var consulta = bancoDados.Jogos
                .Include(e => e.Desenvolvedora)
                .Include(e => e.Categorias)
                .Include(e => e.Produtos).ThenInclude(p => p.Plataforma)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                // ILike: busca sem diferenciar maiúsculas de minúsculas (PostgreSQL)
                consulta = consulta.Where(e => EF.Functions.ILike(e.Titulo, $"%{busca.Trim()}%"));
            }
            return consulta.OrderBy(e => e.Titulo).ToList();
        }

        private Jogo? BuscarCompleto(int id)
        {
            return bancoDados.Jogos
                .Include(e => e.Desenvolvedora)
                .Include(e => e.Publicadora)
                .Include(e => e.Categorias)
                .Include(e => e.Produtos).ThenInclude(p => p.Plataforma)
                .AsSplitQuery()
                .FirstOrDefault(e => e.Id == id);
        }

        private void Aplicar(JogoFormViewModel form, Jogo jogo)
        {
            jogo.Titulo = form.Titulo.Trim();
            jogo.Slug = GerarSlugUnico(jogo.Titulo, jogo.Id);
            jogo.Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim();
            jogo.DataLancamento = form.DataLancamento;
            jogo.ClassificacaoIndicativa = Jogo.Classificacoes.Contains(form.ClassificacaoIndicativa)
                ? form.ClassificacaoIndicativa
                : null;
            jogo.Desenvolvedora = ObterOuCriarEmpresa(form.Desenvolvedora);
            jogo.Publicadora = ObterOuCriarEmpresa(form.Publicadora);
            jogo.Destaque = form.Destaque;
            jogo.Ativo = form.Ativo;

            jogo.Categorias.Clear();
            foreach (var categoria in bancoDados.Categorias.Where(c => form.CategoriaIds.Contains(c.Id)))
            {
                jogo.Categorias.Add(categoria);
            }
        }

        /// <summary>"Elden Ring" -> "elden-ring"; se já existir, "elden-ring-2"...</summary>
        private string GerarSlugUnico(string titulo, int jogoId)
        {
            var baseSlug = SlugHelper.Gerar(titulo);
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = "jogo";
            }

            var slug = baseSlug;
            for (var i = 2; bancoDados.Jogos.Any(e => e.Slug == slug && e.Id != jogoId); i++)
            {
                slug = $"{baseSlug}-{i}";
            }
            return slug;
        }

        /// <summary>Procura a empresa pelo nome; se não existir, cria.</summary>
        private Empresa? ObterOuCriarEmpresa(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return null;
            }

            nome = nome.Trim();
            var slug = SlugHelper.Gerar(nome);
            return bancoDados.Empresas.FirstOrDefault(e => e.Slug == slug)
                   ?? bancoDados.Empresas.Local.FirstOrDefault(e => e.Slug == slug)
                   ?? bancoDados.Empresas.Add(new Empresa { Nome = nome, Slug = slug }).Entity;
        }

        private void PreencherOpcoes(JogoFormViewModel form)
        {
            form.OpcoesCategorias = bancoDados.Categorias.OrderBy(c => c.Nome).ToList();
            form.OpcoesEmpresas = bancoDados.Empresas.OrderBy(e => e.Nome).Select(e => e.Nome).ToList();
        }
    }
}
