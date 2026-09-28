using System.Security.Cryptography;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Cabeçalhos de segurança recomendados (OWASP) em todas as respostas:
    /// <list type="bullet">
    /// <item><b>Content-Security-Policy</b>: o navegador só executa scripts do próprio site ou com o nonce
    /// desta resposta; bloqueia script injetado (XSS), iframes de outros sites e formulários para fora.</item>
    /// <item><b>X-Frame-Options</b> e <c>frame-ancestors</c>: a loja não abre dentro de iframe de outro site (clickjacking).</item>
    /// <item><b>X-Content-Type-Options: nosniff</b>: um arquivo enviado como imagem nunca é tratado como HTML ou script.</item>
    /// <item><b>Referrer-Policy</b>: outros sites só ficam sabendo o domínio de onde o visitante veio, nunca o endereço completo.</item>
    /// <item><b>Permissions-Policy</b>: câmera, microfone, localização e pagamento ficam desligados.</item>
    /// </list>
    /// </summary>
    public class CabecalhosSeguranca
    {
        public const string ChaveNonce = "clouud:nonce-csp";

        private readonly RequestDelegate proximo;
        private readonly bool https;

        public CabecalhosSeguranca(RequestDelegate proximo, bool https)
        {
            this.proximo = proximo;
            this.https = https;
        }

        public Task InvokeAsync(HttpContext http)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            http.Items[ChaveNonce] = nonce;

            var h = http.Response.Headers;
            h["Content-Security-Policy"] = string.Join("; ", new[]
            {
                "default-src 'self'",
                $"script-src 'self' 'nonce-{nonce}'",
                // estilos: os do site, os atributos style="" das páginas e as fontes do Google do tema
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
                "font-src 'self' https://fonts.gstatic.com data:",
                "img-src 'self' data: blob:",
                "connect-src 'self'",
                "object-src 'none'",
                "base-uri 'self'",
                "form-action 'self'",
                "frame-ancestors 'none'",
            }.Concat(https ? new[] { "upgrade-insecure-requests" } : Array.Empty<string>()));
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            h["Cross-Origin-Opener-Policy"] = "same-origin";
            return proximo(http);
        }

        /// <summary>Nonce da resposta atual (usado pelo tag helper dos &lt;script&gt;).</summary>
        public static string? Nonce(HttpContext? http) => http?.Items[ChaveNonce] as string;
    }
}
