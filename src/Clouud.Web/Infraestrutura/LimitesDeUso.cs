using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Limites de uso (rate limiting do próprio ASP.NET) nas ações que um robô atacaria:
    /// login (adivinhar senha), cadastro e "esqueci minha senha" (mandar e-mail para terceiros),
    /// cupom e finalização da compra (adivinhar cupons, travar o estoque). Passou do limite: resposta 429
    /// com a página "Muitas tentativas" e o cabeçalho Retry-After. Os números vêm de "Seguranca:Limites".
    /// </summary>
    public static class LimitesDeUso
    {
        public const string Login = "login";
        public const string FormulariosDeConta = "formularios-de-conta";
        public const string Compras = "compras";

        public static void Configurar(IServiceCollection servicos, IConfiguration configuracao)
        {
            var loginPorMinuto = configuracao.GetValue("Seguranca:Limites:LoginPorMinuto", 10);
            var contaPor15Minutos = configuracao.GetValue("Seguranca:Limites:FormulariosDeContaPor15Minutos", 10);
            var comprasPorMinuto = configuracao.GetValue("Seguranca:Limites:ComprasPorMinuto", 30);

            servicos.AddRateLimiter(opcoes =>
            {
                opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opcoes.OnRejected = (contexto, _) =>
                {
                    if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    {
                        contexto.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();
                    }
                    contexto.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LimitesDeUso))
                        .LogWarning("Limite de uso atingido: {Caminho} de {Ip}", contexto.HttpContext.Request.Path, Ip(contexto.HttpContext));
                    return ValueTask.CompletedTask;
                };

                // Por IP: quem não está logado só é identificado pelo endereço
                opcoes.AddPolicy(Login, http => Janela($"login:{Ip(http)}", loginPorMinuto, TimeSpan.FromMinutes(1)));
                opcoes.AddPolicy(FormulariosDeConta, http => Janela($"conta:{Ip(http)}", contaPor15Minutos, TimeSpan.FromMinutes(15)));
                // Por cliente logado (ou IP)
                opcoes.AddPolicy(Compras, http => Janela(
                    $"compras:{http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Ip(http)}", comprasPorMinuto, TimeSpan.FromMinutes(1)));
            });
        }

        private static RateLimitPartition<string> Janela(string chave, int limite, TimeSpan janela) =>
            RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, limite),
                Window = janela,
                QueueLimit = 0
            });

        /// <summary>IP de quem fez a requisição (atrás de proxy, o ForwardedHeaders em Program.cs já corrige).</summary>
        public static string Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }
}
