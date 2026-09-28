using System.Security.Claims;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Faz o login do usuário gravando o cookie de autenticação com os dados dele (claims).
    /// Usado no login e também depois que o usuário altera os próprios dados, para o
    /// nome e o e-mail no cookie ficarem atualizados.
    /// </summary>
    public class AutenticacaoService
    {
        /// <summary>Claim com o selo de segurança do usuário (conferido a cada requisição em Program.cs).</summary>
        public const string ClaimSelo = "clouud:selo";

        private readonly IHttpContextAccessor httpContextAccessor;

        public AutenticacaoService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task SairAsync()
        {
            var httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Logout fora de uma requisição HTTP.");
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Confere o cookie com o banco: o usuário ainda existe e o selo é o mesmo? Senão a sessão é derrubada
        /// (senha trocada, perfil alterado, conta excluída...). Chamado a cada requisição.
        /// </summary>
        public static async Task ValidarSessaoAsync(CookieValidatePrincipalContext contexto)
        {
            var principal = contexto.Principal;
            var selo = principal?.FindFirstValue(ClaimSelo);
            if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || selo == null)
            {
                await Rejeitar(contexto);
                return;
            }
            var bancoDados = contexto.HttpContext.RequestServices.GetRequiredService<Data.BancoDados>();
            var seloAtual = await bancoDados.Usuarios.Where(u => u.ID == id).Select(u => u.SeloSeguranca).FirstOrDefaultAsync();
            if (seloAtual == null || !string.Equals(seloAtual, selo, StringComparison.Ordinal))
            {
                await Rejeitar(contexto);
            }
        }

        private static async Task Rejeitar(CookieValidatePrincipalContext contexto)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task EntrarAsync(Usuario usuario)
        {
            // nome exibido no topo das páginas
            var nome = usuario.Perfil == PerfilUsuario.Admin ? "Administrador" : usuario.Name;

            //credencial do usuario
            var credencial = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.NameIdentifier, usuario.ID.ToString()),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
                new Claim(ClaimSelo, usuario.SeloSeguranca)
            };

            //configura a identidade de acesso
            var identidade = new ClaimsIdentity(credencial, CookieAuthenticationDefaults.AuthenticationScheme);

            //configura a autenticacao no servidor
            var autenticacaoCookie = new AuthenticationProperties
            {
                AllowRefresh = true,
                IssuedUtc = DateTime.UtcNow, //inicio do tempo
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30), //termino do tempo
            };

            //autentica o usuario no servidor por Cookies
            var httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Login fora de uma requisição HTTP.");
            await httpContext.SignInAsync(new ClaimsPrincipal(identidade), autenticacaoCookie);
        }
    }
}
