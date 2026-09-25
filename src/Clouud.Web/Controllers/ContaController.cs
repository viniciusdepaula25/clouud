using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;


namespace Clouud.Web.Controllers
{
    public class ContaController : Controller
    {
        private readonly BancoDados bancoDados;
        private readonly SenhaService senhas;

        public ContaController(BancoDados bancoDados, SenhaService senhas)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas;
        }

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
            // e-mail sempre em minúsculas e sem espaços, para não existirem duas contas iguais
            var email = conta.Email.Trim().ToLowerInvariant();

            if (ModelState.IsValid && bancoDados.Usuarios.Any(e => e.Email == email))
            {
                ModelState.AddModelError(nameof(conta.Email), "Já existe uma conta com este e-mail");
            }

            //Se os Dados são validos
            if (ModelState.IsValid)
            {
                // Todo cadastro pelo site é de cliente. O primeiro admin é criado ao iniciar a
                // aplicação (seção "AdminInicial" do appsettings).
                Usuario usuario = new Usuario();
                usuario.Name = conta.Nome;
                usuario.Email = email;
                usuario.Perfil = PerfilUsuario.Cliente;
                usuario.Senha = senhas.GerarHash(usuario, conta.Senha); // salva só o hash, nunca a senha
                bancoDados.Usuarios.Add(usuario); //comando insert

                try
                {
                    bancoDados.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    // dois cadastros com o mesmo e-mail ao mesmo tempo: o índice único do banco barra o segundo
                    ModelState.AddModelError(nameof(conta.Email), "Já existe uma conta com este e-mail");
                    return View(conta);
                }

                //Cadastra o Cliente
                Cliente cliente = new Cliente();
                cliente.Id = usuario.ID;
                cliente.Nome = conta.Nome;
                bancoDados.Clientes.Add(cliente);
                bancoDados.SaveChanges();


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
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            if (ModelState.IsValid)
            {
                var email = login.Email.Trim().ToLowerInvariant();
                var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.Email.ToLower() == email);

                var (senhaValida, atualizarHash) = usuario != null
                    ? senhas.Verificar(usuario, login.Senha)
                    : (false, false);

                if (usuario != null && senhaValida && atualizarHash)
                {
                    // conta antiga com senha em texto puro: grava o hash no lugar
                    usuario.Senha = senhas.GerarHash(usuario, login.Senha);
                    bancoDados.SaveChanges();
                }

                if (usuario != null && senhaValida && await AutenticaUsuario(usuario))
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
        public async Task<IActionResult> Logout()
        {
            //desabilita a autenticacao do usuario
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }

        //metodos
        private async Task<bool> AutenticaUsuario(Usuario usuario)
        {
            if (usuario != null)
            {
                string nome = string.Empty;
                switch (usuario.Perfil)
                {
                    case PerfilUsuario.Cliente:
                        var cliente = bancoDados.Clientes.FirstOrDefault(e => e.Id == usuario.ID);
                        // usuário cliente sem registro na tabela Clientes: usa o nome do usuário
                        nome = cliente?.Nome ?? usuario.Name;
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
                await HttpContext.SignInAsync(autenticacaoUsuario, autenticacaoCookie);

                //redireciona para o painel administrativo
                return true;
            }

            return false;
        }

    }

}
