using System.Security.Claims;
using Clouud.Web.Data;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public MinhaContaController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados,
            SenhaService senhas, AutenticacaoService autenticacao, FotoPerfilService fotos) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas;
            this.autenticacao = autenticacao;
            this.fotos = fotos;
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
                Foto = usuario.Foto
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

            var email = conta.Email.Trim().ToLowerInvariant();
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
                return View(conta);
            }

            usuario.Name = conta.Nome.Trim();
            usuario.Email = email;
            if (trocouSenha)
            {
                usuario.Senha = senhas.GerarHash(usuario, conta.NovaSenha!);
            }

            bancoDados.SaveChanges();

            // Atualiza o cookie de login para o novo nome/e-mail aparecerem no topo da página
            await autenticacao.EntrarAsync(usuario);

            TempData["Mensagem"] = "Seus dados foram atualizados.";
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
