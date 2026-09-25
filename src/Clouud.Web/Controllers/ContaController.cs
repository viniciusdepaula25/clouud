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
        private readonly AutenticacaoService autenticacao;

        public ContaController(BancoDados bancoDados, SenhaService senhas, AutenticacaoService autenticacao)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas;
            this.autenticacao = autenticacao;
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
        public async Task<IActionResult> Cadastro(ContaViewModel conta)
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

                // Já entra com a conta nova: não precisa fazer login logo depois de se cadastrar
                await autenticacao.EntrarAsync(usuario);
                TempData["Mensagem"] = $"Conta criada. Bem-vindo(a), {usuario.Name}!";
                return RedirectToAction("Index", "Home", new { area = "Cliente" });
            }

            return View(conta);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            LoginViewModel login = new LoginViewModel { ReturnUrl = returnUrl };
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
                    // Volta para a página que pediu o login, se for do próprio site
                    if (Url.IsLocalUrl(login.ReturnUrl))
                    {
                        return LocalRedirect(login.ReturnUrl);
                    }

                    return usuario.Perfil == PerfilUsuario.Cliente
                        ? RedirectToAction("Index", "Home", new { area = "Cliente" })
                        : RedirectToAction("Index", "Home", new { area = "Admin" });
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
                // grava o cookie de login (nome, e-mail, id e perfil do usuário)
                await autenticacao.EntrarAsync(usuario);
                return true;
            }

            return false;
        }

    }

}
