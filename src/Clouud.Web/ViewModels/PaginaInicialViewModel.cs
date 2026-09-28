using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Página inicial, no estilo das lojas de chaves: destaques e prateleiras de promoções, mais vendidos e lançamentos.</summary>
    public class PaginaInicialViewModel
    {
        public const int ItensPorPrateleira = 4;

        /// <summary>Jogos marcados como destaque no admin (carrossel do topo).</summary>
        public List<ProdutoVitrineViewModel> Destaques { get; set; } = new();
        public List<ProdutoVitrineViewModel> Promocoes { get; set; } = new();
        public List<ProdutoVitrineViewModel> MaisVendidos { get; set; } = new();
        public List<ProdutoVitrineViewModel> Lancamentos { get; set; } = new();

        /// <summary>Plataformas com produto à venda, para o atalho "compre por plataforma".</summary>
        public List<(Plataforma Plataforma, int Produtos)> Plataformas { get; set; } = new();

        /// <summary>Jogos na lista de desejos do cliente logado (vazio para visitantes).</summary>
        public HashSet<int> JogosDesejados { get; set; } = new();

        public bool LojaVazia => Destaques.Count == 0 && Promocoes.Count == 0 && MaisVendidos.Count == 0 && Lancamentos.Count == 0;
    }

    /// <summary>Uma prateleira da página inicial (ex.: "Promoções"), com o link para ver tudo na loja.</summary>
    public record PrateleiraViewModel(string Id, string Titulo, string Descricao, List<ProdutoVitrineViewModel> Produtos,
        string LinkTodos, string TextoTodos, bool ModoCarrinho, HashSet<int> JogosDesejados);
}
