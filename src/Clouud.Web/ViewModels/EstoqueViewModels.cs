using System.ComponentModel.DataAnnotations;
using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Uma linha da visão geral do estoque.</summary>
    public class EstoqueProdutoViewModel
    {
        /// <summary>Abaixo disso o estoque é destacado como baixo.</summary>
        public const int EstoqueBaixo = 5;

        public int ProdutoId { get; set; }
        public string Jogo { get; set; } = string.Empty;
        public string Plataforma { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int Disponiveis { get; set; }
        public int Reservadas { get; set; }
        public int Vendidas { get; set; }
        public int Inativas { get; set; }
    }

    /// <summary>Chaves de um produto, com filtro por situação.</summary>
    public class ChavesProdutoViewModel
    {
        public const int LimiteListagem = 500;

        public EstoqueProdutoViewModel Produto { get; set; } = new();
        public StatusChave? Filtro { get; set; }
        public List<Chave> Chaves { get; set; } = new();

        /// <summary>Total de chaves no filtro (a lista mostra no máximo <see cref="LimiteListagem"/>).</summary>
        public int Total { get; set; }
    }

    /// <summary>Formulário de importação: as chaves coladas, uma por linha.</summary>
    public class ImportarChavesViewModel
    {
        public EstoqueProdutoViewModel Produto { get; set; } = new();

        [Display(Name = "Chaves (uma por linha)")]
        public string? Codigos { get; set; }
    }
}
