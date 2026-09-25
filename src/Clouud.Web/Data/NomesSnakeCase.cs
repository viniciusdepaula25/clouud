using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Clouud.Web.Data
{
    /// <summary>
    /// Deixa os nomes do banco em minúsculo snake_case ("PedidoItens" -> "pedido_itens",
    /// "PrecoPromocional" -> "preco_promocional"). No PostgreSQL, nomes com maiúscula só funcionam entre
    /// aspas ("PedidoItens"); em minúsculo o SQL escrito à mão fica simples: SELECT * FROM pedido_itens.
    /// </summary>
    public static class NomesSnakeCase
    {
        /// <summary>Aplica em tabelas, colunas, chaves primárias e estrangeiras e índices.</summary>
        public static void Aplicar(ModelBuilder modelBuilder)
        {
            foreach (var entidade in modelBuilder.Model.GetEntityTypes())
            {
                var tabela = entidade.GetTableName();
                if (tabela == null)
                {
                    continue;
                }
                entidade.SetTableName(Converter(tabela));

                foreach (var propriedade in entidade.GetProperties())
                {
                    propriedade.SetColumnName(Converter(propriedade.GetColumnName()));
                }
                foreach (var chave in entidade.GetKeys())
                {
                    chave.SetName(Converter(chave.GetName()!));
                }
                foreach (var chaveEstrangeira in entidade.GetForeignKeys())
                {
                    chaveEstrangeira.SetConstraintName(Converter(chaveEstrangeira.GetConstraintName()!));
                }
                foreach (var indice in entidade.GetIndexes())
                {
                    indice.SetDatabaseName(Converter(indice.GetDatabaseName()!));
                }
            }
        }

        /// <summary>"PrecoPromocional" -> "preco_promocional"; "ID" -> "id"; "FK_Chaves_Produtos_ProdutoId" -> "fk_chaves_produtos_produto_id".</summary>
        public static string Converter(string nome)
        {
            var resultado = new StringBuilder(nome.Length + 8);
            for (var i = 0; i < nome.Length; i++)
            {
                var atual = nome[i];
                if (char.IsUpper(atual) && i > 0)
                {
                    var anterior = nome[i - 1];
                    var proximoMinusculo = i + 1 < nome.Length && char.IsLower(nome[i + 1]);
                    // começa palavra nova: "precoPromocional" ou o "U" de "IDUsuario"
                    if (char.IsLower(anterior) || char.IsDigit(anterior) || (char.IsUpper(anterior) && proximoMinusculo))
                    {
                        resultado.Append('_');
                    }
                }
                resultado.Append(char.ToLowerInvariant(atual));
            }
            return resultado.ToString().Replace("__", "_");
        }
    }
}
