using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Galeria de imagens de um jogo.</summary>
    [Authorize(Roles = "Admin")]
    public class GaleriaController : AdminController
    {
        private readonly BancoDados bancoDados;
        private readonly GaleriaService galeria;

        public GaleriaController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, GaleriaService galeria)
            : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.galeria = galeria;
        }

        /// <summary>Galeria do jogo (id do jogo).</summary>
        [HttpGet]
        public IActionResult Index(int id)
        {
            var jogo = bancoDados.Jogos.FirstOrDefault(j => j.Id == id);
            if (jogo == null)
            {
                return NotFound();
            }
            ViewData["Jogo"] = jogo;
            return View(galeria.Listar(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(JogoImagem.MaximoPorJogo * JogoImagem.TamanhoMaximo + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = JogoImagem.MaximoPorJogo * JogoImagem.TamanhoMaximo + 1024 * 1024)]
        public IActionResult Enviar(int id, List<IFormFile> arquivos)
        {
            if (!bancoDados.Jogos.Any(j => j.Id == id))
            {
                return NotFound();
            }
            if (arquivos.Count == 0)
            {
                TempData["Erro"] = "Escolha pelo menos uma imagem.";
                return RedirectToAction("Index", new { id });
            }

            var resultado = galeria.Enviar(id, arquivos);
            if (resultado.Adicionadas > 0)
            {
                TempData["Mensagem"] = resultado.Adicionadas == 1 ? "1 imagem adicionada." : $"{resultado.Adicionadas} imagens adicionadas.";
            }
            if (resultado.Recusadas.Count > 0)
            {
                TempData["Erro"] = "Não entraram: " + string.Join(" · ", resultado.Recusadas);
            }
            return RedirectToAction("Index", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Legenda(int imagemId, string? legenda)
        {
            var imagem = bancoDados.JogoImagens.FirstOrDefault(i => i.Id == imagemId);
            if (imagem == null)
            {
                return NotFound();
            }
            legenda = string.IsNullOrWhiteSpace(legenda) ? null : legenda.Trim();
            if (legenda?.Length > JogoImagem.TamanhoLegenda)
            {
                TempData["Erro"] = $"A legenda pode ter até {JogoImagem.TamanhoLegenda} caracteres.";
            }
            else
            {
                imagem.Legenda = legenda;
                bancoDados.SaveChanges();
                TempData["Mensagem"] = "Legenda salva.";
            }
            return RedirectToAction("Index", new { id = imagem.JogoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Mover(int imagemId, int direcao)
        {
            var imagem = bancoDados.JogoImagens.FirstOrDefault(i => i.Id == imagemId);
            if (imagem == null)
            {
                return NotFound();
            }
            galeria.Mover(imagem, direcao);
            return RedirectToAction("Index", new { id = imagem.JogoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int imagemId)
        {
            var imagem = bancoDados.JogoImagens.FirstOrDefault(i => i.Id == imagemId);
            if (imagem == null)
            {
                return NotFound();
            }
            galeria.Excluir(imagem);
            TempData["Mensagem"] = "Imagem excluída.";
            return RedirectToAction("Index", new { id = imagem.JogoId });
        }
    }
}
