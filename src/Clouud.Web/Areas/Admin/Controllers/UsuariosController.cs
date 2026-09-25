using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : AdminController
    {
        private readonly BancoDados bancoDados;
        private readonly SenhaService senhas;


        public UsuariosController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, SenhaService senhas) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas; 
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
                usuario.Email = usuario.Email.Trim().ToLowerInvariant();
                usuario.Senha = senhas.GerarHash(usuario, usuario.Senha); // salva só o hash
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
                // Se a senha do formulário for diferente do hash salvo, é uma senha nova: gera o hash
                var senhaAtual = bancoDados.Usuarios.AsNoTracking()
                    .Where(e => e.ID == usuario.ID).Select(e => e.Senha).FirstOrDefault();
                if (usuario.Senha != senhaAtual)
                {
                    usuario.Senha = senhas.GerarHash(usuario, usuario.Senha);
                }
                usuario.Email = usuario.Email.Trim().ToLowerInvariant();
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
