using System.Security.Claims;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Controllers
{
    /// <summary>Página pública do jogo e as avaliações dos clientes.</summary>
    [Route("jogo/{slug}")]
    public class JogosController : Controller
    {
        private readonly CatalogoService catalogo;
        private readonly BancoDados bancoDados;

        public JogosController(CatalogoService catalogo, BancoDados bancoDados)
        {
            this.catalogo = catalogo;
            this.bancoDados = bancoDados;
        }

        [HttpGet("")]
        public IActionResult Detalhes(string slug)
        {
            var pagina = catalogo.MontarPaginaJogo(slug, UsuarioLogado());
            return pagina == null ? NotFound() : View(pagina);
        }

        /// <summary>Cria ou atualiza a avaliação do cliente logado.</summary>
        [HttpPost("avaliar")]
        [Authorize(Roles = "Admin,Cliente")]
        [ValidateAntiForgeryToken]
        public IActionResult Avaliar(string slug, AvaliarViewModel form)
        {
            var jogo = bancoDados.Jogos.FirstOrDefault(j => j.Slug == slug && j.Ativo);
            if (jogo == null)
            {
                return NotFound();
            }
            var usuarioId = UsuarioLogado()!.Value;

            string? erro = null;
            if (!catalogo.ComprouOJogo(usuarioId, jogo.Id))
            {
                erro = "Só quem comprou o jogo pode avaliar.";
            }
            else if (form.Nota is < 1 or > 5)
            {
                erro = "Escolha de 1 a 5 estrelas.";
            }
            else if ((form.Comentario?.Trim().Length ?? 0) > Avaliacao.TamanhoComentario)
            {
                erro = $"O comentário pode ter até {Avaliacao.TamanhoComentario} caracteres.";
            }
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return Redirect($"/jogo/{slug}#avaliar");
            }

            var comentario = string.IsNullOrWhiteSpace(form.Comentario) ? null : form.Comentario.Trim();
            var avaliacao = bancoDados.Avaliacoes.FirstOrDefault(a => a.UsuarioId == usuarioId && a.JogoId == jogo.Id);
            if (avaliacao == null)
            {
                bancoDados.Avaliacoes.Add(new Avaliacao
                {
                    UsuarioId = usuarioId,
                    JogoId = jogo.Id,
                    Nota = form.Nota,
                    Comentario = comentario
                });
                TempData["Mensagem"] = "Obrigado pela avaliação!";
            }
            else
            {
                avaliacao.Nota = form.Nota;
                avaliacao.Comentario = comentario;
                avaliacao.AtualizadaEm = DateTime.UtcNow;
                TempData["Mensagem"] = "Avaliação atualizada.";
            }
            bancoDados.SaveChanges();
            return Redirect($"/jogo/{slug}#avaliacoes");
        }

        [HttpPost("avaliacao/excluir")]
        [Authorize(Roles = "Admin,Cliente")]
        [ValidateAntiForgeryToken]
        public IActionResult ExcluirAvaliacao(string slug)
        {
            var usuarioId = UsuarioLogado()!.Value;
            var avaliacao = bancoDados.Avaliacoes.FirstOrDefault(a => a.UsuarioId == usuarioId && a.Jogo.Slug == slug);
            if (avaliacao != null)
            {
                bancoDados.Avaliacoes.Remove(avaliacao);
                bancoDados.SaveChanges();
                TempData["Mensagem"] = "Sua avaliação foi excluída.";
            }
            return Redirect($"/jogo/{slug}#avaliacoes");
        }

        private int? UsuarioLogado() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }
}
