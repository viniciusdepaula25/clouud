using System.Security.Claims;
using Clouud.Web.Data;
using Clouud.Web.Services;
using Clouud.Web.Services.Emails;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Clouud.Web.Infraestrutura;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    /// <summary>Tela "Minha conta": o usuário logado vê e altera os próprios dados.</summary>
    [Authorize(Roles = "Admin,Cliente")]
    public class MinhaContaController : ClienteLoginController
    {
        private readonly BancoDados bancoDados;
        private readonly SenhaService senhas;
        private readonly AutenticacaoService autenticacao;
        private readonly FotoPerfilService fotos;
        private readonly ConfirmacaoEmail confirmacao;

        public MinhaContaController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados,
            SenhaService senhas, AutenticacaoService autenticacao, FotoPerfilService fotos, ConfirmacaoEmail confirmacao)
            : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas;
            this.autenticacao = autenticacao;
            this.fotos = fotos;
            this.confirmacao = confirmacao;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == UsuarioLogadoId());
            if (usuario == null)
            {
                return NotFound();
            }

            return View(new MinhaContaViewModel
            {
                Nome = usuario.Name,
                Email = usuario.Email,
                Foto = usuario.Foto,
                EmailConfirmado = usuario.EmailConfirmadoEm != null,
                ReceberAvisos = usuario.ReceberAvisos
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(MinhaContaViewModel conta)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == UsuarioLogadoId());
            if (usuario == null)
            {
                return NotFound();
            }

            var email = (conta.Email ?? "").Trim().ToLowerInvariant();
            var trocouEmail = email != usuario.Email;
            var trocouSenha = !string.IsNullOrWhiteSpace(conta.NovaSenha);

            if (trocouEmail && bancoDados.Usuarios.Any(e => e.Email == email && e.ID != usuario.ID))
            {
                ModelState.AddModelError(nameof(conta.Email), "Já existe uma conta com este e-mail");
            }

            // Trocar e-mail ou senha exige a senha atual (protege a conta se alguém usar
            // o computador com a sessão aberta)
            if (trocouEmail || trocouSenha)
            {
                if (string.IsNullOrWhiteSpace(conta.SenhaAtual))
                {
                    ModelState.AddModelError(nameof(conta.SenhaAtual), "Informe a senha atual para trocar o e-mail ou a senha");
                }
                else if (!senhas.Verificar(usuario, conta.SenhaAtual).Valida)
                {
                    ModelState.AddModelError(nameof(conta.SenhaAtual), "Senha atual incorreta");
                }
            }

            if (!ModelState.IsValid)
            {
                conta.Foto = usuario.Foto;
                conta.EmailConfirmado = usuario.EmailConfirmadoEm != null;
                return View(conta);
            }

            usuario.Name = conta.Nome.Trim();
            usuario.Email = email;
            usuario.ReceberAvisos = conta.ReceberAvisos;
            if (trocouSenha)
            {
                usuario.Senha = senhas.GerarHash(usuario, conta.NovaSenha!);
            }
            if (trocouEmail)
            {
                // E-mail novo precisa ser confirmado de novo
                usuario.EmailConfirmadoEm = null;
                await confirmacao.AdicionarConfirmacaoAsync(usuario);
            }

            bancoDados.SaveChanges();

            // Atualiza o cookie de login para o novo nome/e-mail aparecerem no topo da página
            await autenticacao.EntrarAsync(usuario);

            TempData["Mensagem"] = trocouEmail
                ? $"Seus dados foram atualizados. Enviamos um link para {usuario.Email} confirmar o novo e-mail."
                : "Seus dados foram atualizados.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Manda de novo o link de confirmação do e-mail (no máximo um a cada 2 minutos).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(LimitesDeUso.FormulariosDeConta)]
        public async Task<IActionResult> ReenviarConfirmacao()
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == UsuarioLogadoId());
            if (usuario == null)
            {
                return NotFound();
            }
            if (usuario.EmailConfirmadoEm != null)
            {
                TempData["Mensagem"] = "Seu e-mail já está confirmado.";
            }
            else if (!confirmacao.PodeReenviar(usuario))
            {
                TempData["Erro"] = "Acabamos de enviar um link. Confira a caixa de entrada (e o spam) e tente de novo em alguns minutos.";
            }
            else
            {
                await confirmacao.AdicionarConfirmacaoAsync(usuario);
                bancoDados.SaveChanges();
                TempData["Mensagem"] = $"Enviamos um novo link para {usuario.Email}.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Troca a foto de perfil (formulário separado: não pede a senha atual).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Foto(IFormFile? arquivo)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == UsuarioLogadoId());
            if (usuario == null)
            {
                return NotFound();
            }

            var (foto, erro) = fotos.Salvar(arquivo);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction(nameof(Index));
            }

            var antiga = usuario.Foto;
            usuario.Foto = foto;
            bancoDados.SaveChanges();
            fotos.Excluir(antiga);

            TempData["Mensagem"] = "Foto atualizada.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoverFoto()
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == UsuarioLogadoId());
            if (usuario == null)
            {
                return NotFound();
            }

            var antiga = usuario.Foto;
            usuario.Foto = null;
            bancoDados.SaveChanges();
            fotos.Excluir(antiga);

            TempData["Mensagem"] = "Foto removida.";
            return RedirectToAction(nameof(Index));
        }

        private int UsuarioLogadoId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }
    }
}
