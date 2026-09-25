using System.Security.Cryptography;
using System.Text;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Gera e confere o hash das senhas dos usuários (PBKDF2, via PasswordHasher do ASP.NET).
    /// A senha digitada nunca é salva no banco, só o hash.
    /// </summary>
    public class SenhaService
    {
        private readonly IPasswordHasher<Usuario> hasher;

        public SenhaService(IPasswordHasher<Usuario> hasher)
        {
            this.hasher = hasher;
        }

        public string GerarHash(Usuario usuario, string senha)
        {
            return hasher.HashPassword(usuario, senha);
        }

        /// <summary>
        /// Confere a senha digitada com a salva no banco.
        /// AtualizarHash = true quando o hash deve ser regravado: senha antiga salva em texto puro
        /// (contas criadas antes desta versão) ou hash gerado com parâmetros antigos.
        /// </summary>
        public (bool Valida, bool AtualizarHash) Verificar(Usuario usuario, string senhaDigitada)
        {
            if (string.IsNullOrEmpty(usuario.Senha))
            {
                return (false, false);
            }

            if (!EhHash(usuario.Senha))
            {
                // Conta antiga com a senha em texto puro: compara e pede para gravar o hash
                var valida = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(usuario.Senha),
                    Encoding.UTF8.GetBytes(senhaDigitada));
                return (valida, valida);
            }

            var resultado = hasher.VerifyHashedPassword(usuario, usuario.Senha, senhaDigitada);
            return (resultado != PasswordVerificationResult.Failed,
                    resultado == PasswordVerificationResult.SuccessRehashNeeded);
        }

        /// <summary>O hash do PasswordHasher é Base64 e começa com o byte 0x00 (formato v2) ou 0x01 (v3).</summary>
        private static bool EhHash(string valor)
        {
            var bytes = new byte[valor.Length];
            if (!Convert.TryFromBase64String(valor, bytes, out var tamanho))
            {
                return false;
            }

            return (bytes[0] == 0x00 && tamanho == 49) || (bytes[0] == 0x01 && tamanho >= 13);
        }
    }
}
