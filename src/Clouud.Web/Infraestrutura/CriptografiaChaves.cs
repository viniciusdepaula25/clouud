using System.Security.Cryptography;
using System.Text;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Criptografia dos códigos das chaves de ativação. No banco, cada código fica:
    /// <list type="bullet">
    /// <item><c>codigo_cifrado</c>: AES-256-GCM (confidencial e à prova de alteração), no formato
    /// <c>v1:base64(nonce | texto cifrado | tag)</c>. O "v1" permite trocar o algoritmo ou a chave no futuro.</item>
    /// <item><c>codigo_hash</c>: HMAC-SHA256 do código, para achar códigos repetidos sem decifrar nada
    /// (o índice único fica nesta coluna).</item>
    /// </list>
    /// As duas chaves de 256 bits são derivadas (HKDF) de uma só chave mestra, a configuração
    /// "Seguranca:ChaveCriptografia" (variável Seguranca__ChaveCriptografia), que nunca fica no banco nem no Git.
    /// Sem ela, os códigos não podem ser lidos: guarde uma cópia em lugar seguro.
    /// </summary>
    public sealed class CriptografiaChaves
    {
        public const string Configuracao = "Seguranca:ChaveCriptografia";
        private const string Prefixo = "v1:";
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;

        private readonly byte[] chaveCifra;
        private readonly byte[] chaveBusca;

        public CriptografiaChaves(byte[] chaveMestra)
        {
            if (chaveMestra.Length != 32)
            {
                throw new ArgumentException("A chave mestra precisa ter 32 bytes (256 bits).", nameof(chaveMestra));
            }
            chaveCifra = HKDF.DeriveKey(HashAlgorithmName.SHA256, chaveMestra, 32, info: Encoding.UTF8.GetBytes("clouud/chaves/cifra/v1"));
            chaveBusca = HKDF.DeriveKey(HashAlgorithmName.SHA256, chaveMestra, 32, info: Encoding.UTF8.GetBytes("clouud/chaves/busca/v1"));
        }

        /// <summary>
        /// Instância usada pelo conversor do Entity Framework (definida ao iniciar a aplicação, em Program.cs).
        /// </summary>
        public static CriptografiaChaves Atual { get; set; } = null!;

        public static CriptografiaChaves DaConfiguracao(IConfiguration configuracao)
        {
            var texto = configuracao[Configuracao];
            if (string.IsNullOrWhiteSpace(texto))
            {
                throw new InvalidOperationException(
                    $"Falta a chave de criptografia das chaves de ativação ({Configuracao}). Gere uma com " +
                    "\"openssl rand -base64 32\" e configure na variável de ambiente Seguranca__ChaveCriptografia ou, " +
                    "em desenvolvimento, com: dotnet user-secrets --project src/Clouud.Web set \"Seguranca:ChaveCriptografia\" \"<a chave>\"");
            }
            byte[] chave;
            try
            {
                chave = Convert.FromBase64String(texto.Trim());
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"{Configuracao} não está em Base64. Gere com \"openssl rand -base64 32\".");
            }
            if (chave.Length != 32)
            {
                throw new InvalidOperationException($"{Configuracao} precisa ter 32 bytes (tem {chave.Length}). Gere com \"openssl rand -base64 32\".");
            }
            return new CriptografiaChaves(chave);
        }

        public static bool EstaCifrado(string valor) => valor.StartsWith(Prefixo, StringComparison.Ordinal);

        public string Cifrar(string codigo)
        {
            var texto = Encoding.UTF8.GetBytes(codigo);
            var saida = new byte[TamanhoNonce + texto.Length + TamanhoTag];
            var nonce = saida.AsSpan(0, TamanhoNonce);
            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(chaveCifra, TamanhoTag);
            aes.Encrypt(nonce, texto, saida.AsSpan(TamanhoNonce, texto.Length), saida.AsSpan(TamanhoNonce + texto.Length),
                Encoding.ASCII.GetBytes(Prefixo));
            return Prefixo + Convert.ToBase64String(saida);
        }

        /// <summary>
        /// Decifra o código. Um valor sem o prefixo "v1:" é de antes da criptografia: volta como está
        /// (e é cifrado na próxima inicialização, por <see cref="ConversaoChavesLegadas"/>).
        /// </summary>
        public string Decifrar(string valor)
        {
            if (!EstaCifrado(valor))
            {
                return valor;
            }
            var dados = Convert.FromBase64String(valor[Prefixo.Length..]);
            var tamanhoTexto = dados.Length - TamanhoNonce - TamanhoTag;
            var texto = new byte[tamanhoTexto];
            using var aes = new AesGcm(chaveCifra, TamanhoTag);
            aes.Decrypt(dados.AsSpan(0, TamanhoNonce), dados.AsSpan(TamanhoNonce, tamanhoTexto),
                dados.AsSpan(TamanhoNonce + tamanhoTexto), texto, Encoding.ASCII.GetBytes(Prefixo));
            return Encoding.UTF8.GetString(texto);
        }

        /// <summary>HMAC-SHA256 do código, em hexadecimal minúsculo (64 caracteres).</summary>
        public string Hash(string codigo) =>
            Convert.ToHexString(HMACSHA256.HashData(chaveBusca, Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
    }
}
