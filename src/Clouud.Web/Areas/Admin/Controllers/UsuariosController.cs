using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : AdminController
    {
        private readonly BancoDados bancoDados;


        public UsuariosController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados; 
        }

        [HttpGet]
        public IActionResult Index()
        {
            //lista todos os usuarios
            var usuarios = bancoDados.Usuarios.ToList();
            //envia a lista de usuarios para a view
            return View(usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string busca)
        {
            //lista os usuarios realizando a busca
            var usuarios = new List<Usuario>();
            if (string.IsNullOrEmpty(busca))
            {
                usuarios = bancoDados.Usuarios.ToList();
            }
            else 
            { 
              usuarios = bancoDados.Usuarios.Where(e => e.Email.Contains(busca)).ToList();
            }
            return View(usuarios);
        }
        [HttpGet]
        public IActionResult Inclui() 
        { 
         Usuario usuario = new Usuario();
            return View(usuario);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(Usuario usuario, IFormFile arquivo)
        {
            if (ModelState.IsValid)
            { 
                bancoDados.Usuarios.Add(usuario);//Incluir
                bancoDados.SaveChanges();//Salva
                //voltar para index
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if(usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(Usuario usuario, IFormFile arquivo)
        {
            if (ModelState.IsValid)
            {
                bancoDados.Usuarios.Update(usuario);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Exibe(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if(usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Exclui(Usuario usuario)
        {
            if(usuario.ID > 0)
            {
                bancoDados.Usuarios.Remove(usuario);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(usuario);
        }
    }
}
