using ProjetoFinal_LojadeJogos.Data;
using ProjetoFinal_LojadeJogos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


namespace ProjetoFinal_LojadeJogos.Controllers
{
    public class ContaController : Controller
    {
        BancoDados bancoDados;
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Cadastro()
        {
            ContaViewModel conta = new ContaViewModel();
            return View(conta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastro(ContaViewModel conta)
        {
            //Se os Dados são validos
            if (ModelState.IsValid)
            {
                Usuario usuario = new Usuario();
                usuario.Name = conta.Nome;
                usuario.Email = conta.Email;
                usuario.Senha = conta.Senha;
                usuario.Perfil = conta.PerfilUsuario;
                //Cadastra Usuário no Banco de Dados
                bancoDados = new BancoDados();
                bancoDados.Usuarios.Add(usuario); //comando insert
                bancoDados.SaveChanges();
                
                if(conta.PerfilUsuario  == PerfilUsuario.Cliente)
                {  
                    //Cadastra o Cliente 
                    Cliente cliente = new Cliente();
                    cliente.Id = usuario.ID;
                    cliente.Nome = conta.Nome;
                    bancoDados.Clientes.Add(cliente);
                    bancoDados.SaveChanges();
                }
     

                return RedirectToAction("Index", "Home");
            }

            return View(conta);
        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginViewModel login = new LoginViewModel();
            return View(login);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel login)
        {
            if (ModelState.IsValid)
            {
                bancoDados = new BancoDados();
                var usuario = bancoDados.Usuarios
                    .FirstOrDefault(e => e.Email == login.Email && e.Senha == login.Senha);

                if (AutenticaUsuario(usuario))
                {
                    if (usuario.Perfil == PerfilUsuario.Cliente)
                    {
                        // Redireciona para a tela desejada para o perfil Cliente
                        return RedirectToAction("Index", "Home", new { area = "Cliente" });
                    }

                    var returnUrl = TempData["returnUrl"]?.ToString();
                    if (string.IsNullOrWhiteSpace(returnUrl))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }

                    return Redirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("Senha", "Usuário ou senha inválidos");
                }
            }
            return View(login);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            //desabilita a autenticacao do usuario
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        //metodos
        public bool AutenticaUsuario(Usuario usuario)
        {
            if (usuario != null)
            {
                bancoDados = new BancoDados();
                string nome = string.Empty;
                switch (usuario.Perfil)
                {
                    case PerfilUsuario.Cliente:
                        var cliente = bancoDados.Clientes.FirstOrDefault(e => e.Id == usuario.ID);
                        nome = cliente.Nome;
                        break;
                    case PerfilUsuario.Admin:
                        nome = "Administrador";
                        break;
                }

                //credencial do usuario
                var credencial = new List<Claim>();
                credencial.Add(new Claim(ClaimTypes.Name, nome));
                credencial.Add(new Claim(ClaimTypes.Email, usuario.Email));
                credencial.Add(new Claim(ClaimTypes.NameIdentifier, usuario.ID.ToString()));
                credencial.Add(new Claim(ClaimTypes.Role, usuario.Perfil.ToString()));

                //configura a identidade de acesso
                var identidade = new ClaimsIdentity(credencial,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                //configura a autenticacao no servidor
                var autenticacaoCookie = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IssuedUtc = DateTime.UtcNow, //inicio do tempo 
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30), //termino do tempo
                    //RedirectUri = @"~/"
                };

                //autentica o usuario no servidor por Cookies
                var autenticacaoUsuario = new ClaimsPrincipal(identidade);
                HttpContext.SignInAsync(autenticacaoUsuario, autenticacaoCookie);

                //redireciona para o painel administrativo
                return true;
            }

            return false;
        }

    }

}