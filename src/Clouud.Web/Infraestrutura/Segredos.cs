using Npgsql;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Senhas e chaves da aplicação. Nenhuma fica nos arquivos appsettings (que vão para o Git):
    /// vêm de variáveis de ambiente (produção, Docker, CI) ou do user-secrets do .NET (desenvolvimento).
    /// </summary>
    public static class Segredos
    {
        /// <summary>
        /// Connection string do PostgreSQL. O appsettings tem só servidor, banco e usuário; a senha vem de
        /// "Banco:Senha". Quem preferir pode passar a connection string inteira em ConnectionStrings__LojaJogos.
        /// </summary>
        public static string MontarConexaoBanco(IConfiguration configuracao)
        {
            var texto = configuracao.GetConnectionString("LojaJogos");
            if (string.IsNullOrWhiteSpace(texto))
            {
                throw new InvalidOperationException(
                    "Falta a connection string \"LojaJogos\" (seção ConnectionStrings do appsettings.json ou variável ConnectionStrings__LojaJogos).");
            }
            var conexao = new NpgsqlConnectionStringBuilder(texto);
            var senha = configuracao["Banco:Senha"];
            if (!string.IsNullOrEmpty(senha))
            {
                conexao.Password = senha;
            }
            return conexao.ConnectionString;
        }

        /// <summary>Avisa no log (e barra em produção) o que estiver faltando ou inseguro.</summary>
        public static void ConferirAoIniciar(IConfiguration configuracao, IWebHostEnvironment ambiente, ILogger logger)
        {
            var conexao = new NpgsqlConnectionStringBuilder(MontarConexaoBanco(configuracao));
            if (string.IsNullOrEmpty(conexao.Password))
            {
                logger.LogWarning(
                    "A senha do banco não foi configurada. Defina a variável de ambiente Banco__Senha ou, em desenvolvimento, " +
                    "rode: dotnet user-secrets --project src/Clouud.Web set \"Banco:Senha\" \"sua-senha\"");
            }

            if (!ambiente.IsDevelopment())
            {
                if (configuracao["AllowedHosts"] is null or "*")
                {
                    logger.LogWarning("AllowedHosts está como \"*\". Em produção, informe o domínio da loja (ex.: AllowedHosts=clouud.com.br).");
                }
                if (configuracao["Loja:UrlPublica"]?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true)
                {
                    logger.LogWarning("Loja:UrlPublica usa http://. Os links dos e-mails devem usar https:// em produção.");
                }
            }
        }
    }
}
