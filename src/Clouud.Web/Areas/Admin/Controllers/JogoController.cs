using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
            //lista todos os usuarios
            var jogos = bancoDados.Jogos.ToList();
            //envia a lista de usuarios para a view
            return View(jogos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string busca)
        {
            //lista os usuarios realizando a busca
            var jogos = new List<Jogo>();
            if (string.IsNullOrWhiteSpace(busca))
            {
                jogos = bancoDados.Jogos.ToList();
            }
            else
            {
                jogos = bancoDados.Jogos.Where(e => e.Nome.Contains(busca)).ToList();
            }
            return View(jogos);
        }

        [HttpGet]
        public IActionResult Inclui()
        {  
            Jogo jogo = new Jogo();
            return View(jogo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(Jogo jogo, IFormFile arquivo)
        {
            if (ModelState.IsValid)
            {
                //Verifica se existe arquivo
                if(arquivo != null)
                {
                    var nomeArquivo = SalvaArquivo(arquivo);
                    jogo.Foto = nomeArquivo;
                }

                bancoDados.Jogos.Add(jogo);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(jogo);
        }
        [HttpGet]
        public IActionResult Altera(int id)
        {
            var jogo = bancoDados.Jogos.FirstOrDefault(e => e.Id == id);
            if (jogo == null)
            {
                return NotFound();
            }
            return View(jogo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(Jogo jogo, IFormFile arquivo)
        {
            if (ModelState.IsValid)
            {
                //verifica se existe um arquivo
                if (arquivo != null)
                {
                    //exclui o arquivo antigo
                    ExcluiArquivo(jogo.Foto);
                    //salva o arquivo novo
                    var nomeArquivo = SalvaArquivo(arquivo);
                    jogo.Foto = nomeArquivo;
                }
                bancoDados.Jogos.Update(jogo);
                bancoDados.SaveChanges();
                //volta para index
                return RedirectToAction("Index");
            }
            return View(jogo);
        }

        [HttpGet]
        public IActionResult Exibe(int id)
        {
            var jogo = bancoDados.Jogos.FirstOrDefault(e => e.Id == id);
            if (jogo == null)
            {
                return NotFound();
            }
            return View(jogo);
        }
        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var jogo = bancoDados.Jogos.FirstOrDefault(e => e.Id == id);
            if (jogo == null)
            {
                return NotFound();
            }
            return View(jogo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Exclui(Jogo jogo)
        {
            if (jogo.Id > 0)
            {
                bancoDados.Jogos.Remove(jogo);
                bancoDados.SaveChanges();
                //volta para index
                return RedirectToAction("Index");
            }
            return View(jogo);
        }
    }
}
