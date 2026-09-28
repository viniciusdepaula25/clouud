using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.Services.Emails;
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
        private readonly ConfirmacaoEmail confirmacao;
        private readonly RedefinicaoSenhaService redefinicao;

        public ContaController(BancoDados bancoDados, SenhaService senhas, AutenticacaoService autenticacao,
            ConfirmacaoEmail confirmacao, RedefinicaoSenhaService redefinicao)
        {
            this.redefinicao = redefinicao;
            this.bancoDados = bancoDados;
            this.senhas = senhas;
            this.autenticacao = autenticacao;
            this.confirmacao = confirmacao;
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

                // E-mail de boas-vindas com o link para confirmar o endereço (vai pela fila, em segundo plano)
                await confirmacao.AdicionarBoasVindasAsync(usuario);
                bancoDados.SaveChanges();

                // Já entra com a conta nova: não precisa fazer login logo depois de se cadastrar
                await autenticacao.EntrarAsync(usuario);
                TempData["Mensagem"] = $"Conta criada. Bem-vindo(a), {usuario.Name}! Enviamos um e-mail para {usuario.Email} para confirmar o endereço.";
                return RedirectToAction("Index", "Home", new { area = "" });
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
                        ? RedirectToAction("Index", "Home", new { area = "" })
                        : RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                else
                {
                    ModelState.AddModelError("Senha", "Usuário ou senha inválidos");
                }
            }
            return View(login);
        }

        /// <summary>Link do e-mail de confirmação (funciona mesmo sem estar logado).</summary>
        [HttpGet("/conta/confirmar-email")]
        public IActionResult ConfirmarEmail(string? codigo)
        {
            var usuario = confirmacao.Confirmar(codigo);
            return View(usuario);
        }

        [HttpGet("/conta/esqueci-senha")]
        public IActionResult EsqueciSenha() => View(new EsqueciSenhaViewModel());

        [HttpPost("/conta/esqueci-senha")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EsqueciSenha(EsqueciSenhaViewModel pedido)
        {
            if (!ModelState.IsValid)
            {
                return View(pedido);
            }
            await redefinicao.SolicitarAsync(pedido.Email);
            // Mesma resposta com ou sem conta: não revela quais e-mails são cadastrados
            ViewData["Enviado"] = pedido.Email.Trim().ToLowerInvariant();
            return View(new EsqueciSenhaViewModel());
        }

        [HttpGet("/conta/redefinir-senha")]
        public IActionResult RedefinirSenha(string? token)
        {
            NaoRepassarEndereco();
            var pedido = redefinicao.Validar(token);
            if (pedido == null)
            {
                return View("LinkSenhaInvalido");
            }
            ViewData["Email"] = pedido.Usuario.Email;
            return View(new RedefinirSenhaViewModel { Token = token! });
        }

        [HttpPost("/conta/redefinir-senha")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedefinirSenha(RedefinirSenhaViewModel form)
        {
            NaoRepassarEndereco();
            var pedido = redefinicao.Validar(form.Token);
            if (pedido == null)
            {
                return View("LinkSenhaInvalido");
            }
            if (!ModelState.IsValid)
            {
                ViewData["Email"] = pedido.Usuario.Email;
                return View(form);
            }

            await redefinicao.RedefinirAsync(form.Token, form.NovaSenha);
            TempData["Mensagem"] = "Senha alterada. Entre com a nova senha.";
            return Redirect("/conta/login");
        }

        /// <summary>Link "parar de receber" dos avisos da lista de desejos: confirma com um botão (sem login).</summary>
        [HttpGet("/conta/parar-avisos")]
        public IActionResult PararAvisos(string? codigo, [FromServices] DescadastroAvisos descadastro)
        {
            var usuario = BuscarPeloCodigo(codigo, descadastro);
            ViewData["Codigo"] = codigo;
            return View(usuario);
        }

        [HttpPost("/conta/parar-avisos")]
        [ValidateAntiForgeryToken]
        public IActionResult PararAvisosConfirmar(string? codigo, [FromServices] DescadastroAvisos descadastro)
        {
            var usuario = BuscarPeloCodigo(codigo, descadastro);
            if (usuario != null)
            {
                usuario.ReceberAvisos = false;
                bancoDados.SaveChanges();
                ViewData["Parou"] = true;
            }
            return View("PararAvisos", usuario);
        }

        private Usuario? BuscarPeloCodigo(string? codigo, DescadastroAvisos descadastro)
        {
            var id = descadastro.LerUsuario(codigo);
            return id == null ? null : bancoDados.Usuarios.FirstOrDefault(u => u.ID == id);
        }

        /// <summary>O endereço da página tem o código do link: não manda para outros sites (cabeçalho Referer).</summary>
        private void NaoRepassarEndereco() => Response.Headers["Referrer-Policy"] = "no-referrer";

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
