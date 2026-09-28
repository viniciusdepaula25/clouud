using Clouud.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Cifra as chaves que ainda estão em texto puro (bancos de antes da criptografia). Roda ao iniciar a
    /// aplicação, depois das migrations; nas próximas inicializações não encontra nada e termina na hora.
    /// </summary>
    public static class ConversaoChavesLegadas
    {
        private const int Lote = 500;

        private sealed record Linha(int Id, string Codigo);

        public static async Task ConverterAsync(IServiceProvider servicos)
        {
            using var escopo = servicos.CreateScope();
            var bancoDados = escopo.ServiceProvider.GetRequiredService<BancoDados>();
            var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ConversaoChavesLegadas));
            var cripto = CriptografiaChaves.Atual;

            try
            {
                if ((await bancoDados.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogWarning("O banco tem migrations pendentes; as chaves em texto puro serão cifradas depois de 'dotnet ef database update'.");
                    return;
                }

                var total = 0;
                while (true)
                {
                    var linhas = await bancoDados.Database
                        .SqlQuery<Linha>($"""
                            SELECT id AS "Id", codigo_cifrado AS "Codigo" FROM chaves
                            WHERE codigo_cifrado NOT LIKE 'v1:%' OR codigo_hash IS NULL
                            ORDER BY id LIMIT {Lote}
                            """)
                        .ToListAsync();
                    if (linhas.Count == 0)
                    {
                        break;
                    }

                    await using var transacao = await bancoDados.Database.BeginTransactionAsync();
                    foreach (var linha in linhas)
                    {
                        var codigo = cripto.Decifrar(linha.Codigo); // texto puro volta como está
                        await bancoDados.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE chaves SET codigo_cifrado = {cripto.Cifrar(codigo)}, codigo_hash = {cripto.Hash(codigo)} WHERE id = {linha.Id}");
                    }
                    await transacao.CommitAsync();
                    total += linhas.Count;
                }

                if (total > 0)
                {
                    logger.LogWarning("{Quantidade} chave(s) de ativação em texto puro foram cifradas.", total);
                }
            }
            catch (Exception erro)
            {
                logger.LogError(erro, "Não foi possível cifrar as chaves em texto puro. O banco está acessível?");
            }
        }
    }
}
