using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>Resultado da importação de chaves.</summary>
    public class ResultadoImportacao
    {
        public int Adicionadas { get; set; }

        /// <summary>Chaves que apareceram mais de uma vez no texto colado (entram só uma vez).</summary>
        public List<string> RepetidasNoTexto { get; } = new();

        /// <summary>Chaves que já estavam cadastradas (neste ou em outro produto).</summary>
        public List<string> JaCadastradas { get; } = new();

        /// <summary>Linhas maiores que o tamanho máximo do código.</summary>
        public List<string> Invalidas { get; } = new();

        public string? Erro { get; set; }
    }

    /// <summary>Regras do estoque de chaves.</summary>
    public class EstoqueService
    {
        public const int MaximoPorImportacao = 5000;

        private readonly BancoDados bancoDados;

        public EstoqueService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        /// <summary>
        /// Importa as chaves coladas pelo admin, uma por linha. Linhas vazias são ignoradas, espaços nas
        /// pontas são removidos e chaves repetidas ou já cadastradas não entram de novo.
        /// </summary>
        public ResultadoImportacao Importar(int produtoId, string? texto)
        {
            var resultado = new ResultadoImportacao();
            var linhas = (texto ?? string.Empty)
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            if (linhas.Count == 0)
            {
                resultado.Erro = "Cole pelo menos uma chave (uma por linha).";
                return resultado;
            }
            if (linhas.Count > MaximoPorImportacao)
            {
                resultado.Erro = $"Importe no máximo {MaximoPorImportacao} chaves por vez (foram coladas {linhas.Count}).";
                return resultado;
            }

            var novas = new List<string>();
            var vistas = new HashSet<string>();
            foreach (var codigo in linhas)
            {
                if (codigo.Length > Chave.TamanhoMaximoCodigo)
                {
                    resultado.Invalidas.Add(codigo);
                }
                else if (!vistas.Add(codigo))
                {
                    resultado.RepetidasNoTexto.Add(codigo);
                }
                else
                {
                    novas.Add(codigo);
                }
            }

            var existentes = bancoDados.Chaves
                .Where(c => novas.Contains(c.Codigo))
                .Select(c => c.Codigo)
                .ToHashSet();
            resultado.JaCadastradas.AddRange(novas.Where(existentes.Contains));

            var agora = DateTime.UtcNow;
            foreach (var codigo in novas.Where(c => !existentes.Contains(c)))
            {
                bancoDados.Chaves.Add(new Chave { ProdutoId = produtoId, Codigo = codigo, AdicionadaEm = agora });
                resultado.Adicionadas++;
            }

            try
            {
                bancoDados.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Outra importação cadastrou alguma dessas chaves ao mesmo tempo
                bancoDados.ChangeTracker.Clear();
                resultado.Adicionadas = 0;
                resultado.Erro = "Algumas chaves foram cadastradas por outra pessoa agora mesmo. Nada foi importado; tente de novo.";
            }
            return resultado;
        }

        /// <summary>Quantidade de chaves disponíveis por produto.</summary>
        public Dictionary<int, int> DisponiveisPorProduto()
        {
            return bancoDados.Chaves
                .Where(c => c.Status == StatusChave.Disponivel)
                .GroupBy(c => c.ProdutoId)
                .Select(g => new { g.Key, Quantidade = g.Count() })
                .ToDictionary(x => x.Key, x => x.Quantidade);
        }
    }
}
