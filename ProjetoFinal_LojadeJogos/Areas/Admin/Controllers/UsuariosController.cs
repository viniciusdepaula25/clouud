using ProjetoFinal_LojadeJogos.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace ProjetoFinal_LojadeJogos.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : AdminController
    {
        BancoDados bancoDados;

        public UsuariosController(IWebHostEnvironment webHostEnvironment) : base(webHostEnvironment)
        { 
        }

        [HttpGet]
        public IActionResult Index()
        {
            //Inicializa o banco de dados
            bancoDados = new BancoDados();
            //lista todos os usuarios
            var usuarios = bancoDados.Usuarios.ToList();
            //envia a lista de usuarios para a view
            return View(usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string busca)
        {
            //inicializa o banco de dados
            bancoDados = new BancoDados();
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
                //inclui o usuario no banco de dados
                bancoDados = new BancoDados();
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
            //obtem o usuario no banco de dados
            bancoDados = new BancoDados();
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
                bancoDados = new BancoDados();
                bancoDados.Usuarios.Update(usuario);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Exibe(int id)
        {
            bancoDados = new BancoDados();
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
            bancoDados = new BancoDados();
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
                bancoDados = new BancoDados();
                bancoDados.Usuarios.Remove(usuario);
                bancoDados.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(usuario);
        }
    }
}
