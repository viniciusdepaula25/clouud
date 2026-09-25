using System.Security.Claims;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Faz o login do usuário gravando o cookie de autenticação com os dados dele (claims).
    /// Usado no login e também depois que o usuário altera os próprios dados, para o
    /// nome e o e-mail no cookie ficarem atualizados.
    /// </summary>
    public class AutenticacaoService
    {
        private readonly BancoDados bancoDados;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AutenticacaoService(BancoDados bancoDados, IHttpContextAccessor httpContextAccessor)
        {
            this.bancoDados = bancoDados;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task EntrarAsync(Usuario usuario)
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
            var credencial = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.NameIdentifier, usuario.ID.ToString()),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
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
