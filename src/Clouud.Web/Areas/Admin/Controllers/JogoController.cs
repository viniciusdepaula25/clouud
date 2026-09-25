using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clouud.Web.Areas.Admin.Controllers
    
{
    [Authorize(Roles = "Admin")]

    public class JogoController : AdminController
    {
        BancoDados bancoDados;
        public JogoController(IWebHostEnvironment webHostEnvironment) : base(webHostEnvironment)
        {  
        }

        [HttpGet]
        public IActionResult Index()
        {
            //inicializa o banco de dados
            bancoDados = new BancoDados();
            //lista todos os usuarios
            var jogos = bancoDados.Jogos.ToList();
            //envia a lista de usuarios para a view
            return View(jogos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string busca)
        {
            //inicializa o banco de dados
            bancoDados = new BancoDados();
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

                bancoDados = new BancoDados();
                bancoDados.Jogos.Add(jogo);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(jogo);
        }
        [HttpGet]
        public IActionResult Altera(int id)
        {
            //obtem usuario do banco de dados
            bancoDados = new BancoDados();
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
                //altera o usuario no banco de dados
                bancoDados = new BancoDados();
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
            //obtem usuario do banco de dados
            bancoDados = new BancoDados();
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
            //obtem usuario do banco de dados
            bancoDados = new BancoDados();
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
                //exclui usuario do banco de dados
                bancoDados = new BancoDados();
                bancoDados.Jogos.Remove(jogo);
                bancoDados.SaveChanges();
                //volta para index
                return RedirectToAction("Index");
            }
            return View(jogo);
        }
    }
}
