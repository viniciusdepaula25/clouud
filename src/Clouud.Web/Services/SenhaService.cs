using System.Security.Cryptography;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Gera e confere o hash das senhas dos usuários (PBKDF2, via PasswordHasher do ASP.NET).
    /// A senha digitada nunca é salva no banco, só o hash.
    /// </summary>
    public class SenhaService
    {
        /// <summary>Tamanho mínimo de senha (cadastro, troca, "esqueci minha senha" e admin inicial).</summary>
        public const int TamanhoMinimo = 8;

        private readonly IPasswordHasher<Usuario> hasher;

        /// <summary>Hash de uma senha qualquer, para o login de e-mail inexistente levar o mesmo tempo.</summary>
        private static string? hashFalso;

        public SenhaService(IPasswordHasher<Usuario> hasher)
        {
            this.hasher = hasher;
        }

        public string GerarHash(Usuario usuario, string senha)
        {
            return hasher.HashPassword(usuario, senha);
        }

        /// <summary>
        /// Confere a senha digitada com o hash salvo no banco.
        /// AtualizarHash = true quando o hash foi gerado com parâmetros antigos e deve ser regravado.
        /// </summary>
        public (bool Valida, bool AtualizarHash) Verificar(Usuario usuario, string senhaDigitada)
        {
            if (!EhHash(usuario.Senha))
            {
                return (false, false); // nunca compara com texto puro (ver ConverterSenhasLegadasAsync)
            }

            var resultado = hasher.VerifyHashedPassword(usuario, usuario.Senha, senhaDigitada);
            return (resultado != PasswordVerificationResult.Failed,
                    resultado == PasswordVerificationResult.SuccessRehashNeeded);
        }

        /// <summary>
        /// Faz o mesmo trabalho de conferir uma senha, sem usuário. Assim o login com e-mail que não existe
        /// demora o mesmo que com e-mail existente, e o tempo de resposta não revela quem tem conta.
        /// </summary>
        public void GastarTempoComoVerificacao(string senhaDigitada)
        {
            var falso = new Usuario();
            hashFalso ??= hasher.HashPassword(falso, Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
            hasher.VerifyHashedPassword(falso, hashFalso, senhaDigitada);
        }

        /// <summary>O hash do PasswordHasher é Base64 e começa com o byte 0x00 (formato v2) ou 0x01 (v3).</summary>
        public static bool EhHash(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return false;
            }
            var bytes = new byte[valor.Length];
            if (!Convert.TryFromBase64String(valor, bytes, out var tamanho))
            {
                return false;
            }

            return (bytes[0] == 0x00 && tamanho == 49) || (bytes[0] == 0x01 && tamanho >= 13);
        }

        /// <summary>
        /// Contas do projeto original tinham a senha em texto puro no banco. Ao iniciar, a aplicação troca
        /// cada uma pelo hash; o login nunca mais compara texto puro.
        /// </summary>
        public static async Task ConverterSenhasLegadasAsync(IServiceProvider servicos)
        {
            using var escopo = servicos.CreateScope();
            var bancoDados = escopo.ServiceProvider.GetRequiredService<BancoDados>();
            var senhas = escopo.ServiceProvider.GetRequiredService<SenhaService>();
            var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SenhaService));
            try
            {
                if ((await bancoDados.Database.GetPendingMigrationsAsync()).Any())
                {
                    return;
                }
                var convertidas = 0;
                foreach (var usuario in await bancoDados.Usuarios.ToListAsync())
                {
                    if (!EhHash(usuario.Senha) && !string.IsNullOrEmpty(usuario.Senha))
                    {
                        usuario.Senha = senhas.GerarHash(usuario, usuario.Senha);
                        convertidas++;
                    }
                }
                if (convertidas > 0)
                {
                    await bancoDados.SaveChangesAsync();
                    logger.LogWarning("{Quantidade} senha(s) em texto puro foram trocadas pelo hash.", convertidas);
                }
            }
            catch (Exception erro)
            {
                logger.LogError(erro, "Não foi possível converter as senhas em texto puro. O banco está acessível?");
            }
        }
    }
}
