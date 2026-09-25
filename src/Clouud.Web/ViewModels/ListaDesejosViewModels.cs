namespace Clouud.Web.ViewModels
{
    /// <summary>Um jogo da lista de desejos, com os produtos à venda agora.</summary>
    public class DesejoViewModel
    {
        public int JogoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Capa { get; set; }
        public bool JogoAtivo { get; set; }
        public DateTime AdicionadoEm { get; set; }
        public List<ProdutoVitrineViewModel> Produtos { get; set; } = new();

        /// <summary>Menor preço entre os produtos com chave disponível.</summary>
        public decimal? MenorPreco => Produtos.Where(p => !p.Esgotado).Select(p => (decimal?)p.PrecoAtual).Min();
        public bool TemPromocao => Produtos.Any(p => p.EmPromocao && !p.Esgotado);
        public bool Disponivel => Produtos.Any(p => !p.Esgotado);
    }
}
