using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Filtros da loja, lidos da URL (/loja?busca=...&amp;ordem=menor-preco&amp;pagina=2).</summary>
    public class FiltroVitrine
    {
        public const int TamanhoPagina = 24;

        /// <summary>Opções de ordenação (valor na URL → texto no seletor).</summary>
        public static readonly IReadOnlyList<(string Valor, string Texto)> Ordens =
        [
            ("destaques", "Destaques"),
            ("menor-preco", "Menor preço"),
            ("maior-preco", "Maior preço"),
            ("lancamentos", "Lançamentos"),
            ("mais-vendidos", "Mais vendidos"),
        ];

        public string? Busca { get; set; }
        public string? Plataforma { get; set; }
        public string? Categoria { get; set; }
        public bool Promocoes { get; set; }
        public string? Ordem { get; set; }
        public decimal? PrecoMin { get; set; }
        public decimal? PrecoMax { get; set; }
        public int Pagina { get; set; } = 1;

        /// <summary>Ordem válida (a desconhecida vira "destaques").</summary>
        public string OrdemEfetiva => Ordens.Any(o => o.Valor == Ordem) ? Ordem! : "destaques";

        /// <summary>Tem algum filtro além da ordem e da página?</summary>
        public bool TemFiltro => !string.IsNullOrWhiteSpace(Busca) || !string.IsNullOrWhiteSpace(Plataforma)
                                 || !string.IsNullOrWhiteSpace(Categoria) || Promocoes || PrecoMin.HasValue || PrecoMax.HasValue;
    }

    /// <summary>Vitrine da loja: uma página de produtos à venda e os filtros escolhidos.</summary>
    public class VitrineViewModel
    {
        public FiltroVitrine Filtro { get; set; } = new();

        public string? Busca => Filtro.Busca;
        public string? Plataforma => Filtro.Plataforma;
        public string? Categoria => Filtro.Categoria;
        public bool SomentePromocoes => Filtro.Promocoes;

        /// <summary>Produtos da página atual.</summary>
        public List<ProdutoVitrineViewModel> Produtos { get; set; } = new();

        /// <summary>Total de produtos encontrados (todas as páginas).</summary>
        public int TotalProdutos { get; set; }
        public int Pagina { get; set; } = 1;
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalProdutos / (double)FiltroVitrine.TamanhoPagina));
        public int PrimeiroDaPagina => TotalProdutos == 0 ? 0 : (Pagina - 1) * FiltroVitrine.TamanhoPagina + 1;
        public int UltimoDaPagina => Math.Min(Pagina * FiltroVitrine.TamanhoPagina, TotalProdutos);
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
        public string Slug { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Capa { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;
        public List<string> Categorias { get; set; } = new();
        public string? Desenvolvedora { get; set; }
        public DateOnly? DataLancamento { get; set; }
        public bool Destaque { get; set; }

        /// <summary>Unidades vendidas em pedidos pagos (para "mais vendidos").</summary>
        public int Vendidos { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoAtual { get; set; }

        /// <summary>Média das avaliações do jogo (nulo sem avaliações).</summary>
        public double? MediaAvaliacoes { get; set; }
        public int TotalAvaliacoes { get; set; }

        /// <summary>Chaves disponíveis no estoque.</summary>
        public int Disponiveis { get; set; }

        public bool Esgotado => Disponiveis == 0;

        public bool EmPromocao => PrecoAtual < Preco;
        public int PercentualDesconto => Preco > 0 && EmPromocao ? (int)Math.Round((1 - PrecoAtual / Preco) * 100) : 0;
    }

    /// <summary>Um card de produto e o que ele precisa saber da página onde aparece.</summary>
    public record CardProdutoViewModel(ProdutoVitrineViewModel Produto, bool ModoCarrinho, bool Desejado, string VoltarPara);
}
