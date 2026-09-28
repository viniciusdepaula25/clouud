namespace Clouud.Web.ViewModels
{
    /// <summary>Uma linha do carrinho, com o preço e o estoque de agora.</summary>
    public class CarrinhoLinhaViewModel
    {
        public int ProdutoId { get; set; }
        public int JogoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Capa { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoAtual { get; set; }
        public int Disponiveis { get; set; }

        /// <summary>Produto, jogo e plataforma ativos.</summary>
        public bool AVenda { get; set; }

        public decimal Subtotal => PrecoAtual * Quantidade;
        public bool EmPromocao => PrecoAtual < Preco;

        /// <summary>O que impede a compra desta linha, se houver.</summary>
        public string? Problema =>
            !AVenda ? "Não está mais à venda. Remova do carrinho."
            : Disponiveis == 0 ? "Esgotado. Remova do carrinho."
            : Quantidade > Disponiveis ? $"Só restam {Disponiveis}. Diminua a quantidade."
            : null;
    }

    public class CarrinhoViewModel
    {
        public List<CarrinhoLinhaViewModel> Itens { get; set; } = new();
        public decimal Subtotal => Itens.Sum(i => i.Subtotal);

        /// <summary>Cupom guardado para este carrinho (vale ou não, conforme <see cref="ErroCupom"/>).</summary>
        public string? CodigoCupom { get; set; }
        public string? DescricaoCupom { get; set; }
        public decimal Desconto { get; set; }

        /// <summary>Por que o cupom guardado não vale agora (ex.: pedido mínimo). Nulo se vale.</summary>
        public string? ErroCupom { get; set; }

        public decimal Total => Subtotal - Desconto;
        public bool PodeFinalizar => Itens.Count > 0 && Itens.All(i => i.Problema == null);
    }
}
