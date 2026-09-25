using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Vitrine da loja: produtos à venda e os filtros escolhidos.</summary>
    public class VitrineViewModel
    {
        public string? Busca { get; set; }
        public string? Plataforma { get; set; }
        public string? Categoria { get; set; }
        public bool SomentePromocoes { get; set; }

        public List<ProdutoVitrineViewModel> Produtos { get; set; } = new();
        public List<Plataforma> Plataformas { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();

        /// <summary>Jogos na lista de desejos do cliente logado (vazio para visitantes).</summary>
        public HashSet<int> JogosDesejados { get; set; } = new();
    }

    /// <summary>Um card da vitrine (um produto: jogo + plataforma + edição).</summary>
    public class ProdutoVitrineViewModel
    {
        public int ProdutoId { get; set; }
        public int JogoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Capa { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;
        public List<string> Categorias { get; set; } = new();
        public string? Desenvolvedora { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoAtual { get; set; }

        /// <summary>Chaves disponíveis no estoque.</summary>
        public int Disponiveis { get; set; }

        public bool Esgotado => Disponiveis == 0;

        public bool EmPromocao => PrecoAtual < Preco;
        public int PercentualDesconto => Preco > 0 && EmPromocao ? (int)Math.Round((1 - PrecoAtual / Preco) * 100) : 0;
    }
}
