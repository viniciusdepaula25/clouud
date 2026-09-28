using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Tela inicial do admin: números do período escolhido e o que precisa de atenção agora.</summary>
    public class PainelViewModel
    {
        public static readonly int[] PeriodosPermitidos = [7, 30, 90];

        public int Dias { get; set; }

        // Período escolhido (pedidos pagos; reembolsados não entram)
        public decimal Faturamento { get; set; }
        public decimal FaturamentoAnterior { get; set; }
        public int PedidosPagos { get; set; }
        public int PedidosPagosAnterior { get; set; }
        public int ChavesVendidas { get; set; }
        public decimal TicketMedio => PedidosPagos == 0 ? 0 : Faturamento / PedidosPagos;

        public List<VendaDiaViewModel> VendasPorDia { get; set; } = new();
        public List<MaisVendidoViewModel> MaisVendidos { get; set; } = new();

        // Situação atual
        public int AguardandoPagamento { get; set; }
        public decimal ValorAguardando { get; set; }
        public int ProdutosEsgotados { get; set; }
        public int ChavesDisponiveis { get; set; }
        public int Clientes { get; set; }
        public List<EstoqueProdutoViewModel> Repor { get; set; } = new();
        public List<Pedido> UltimosPedidos { get; set; } = new();

        /// <summary>Variação em % contra o período anterior de mesmo tamanho; nulo quando não há base de comparação.</summary>
        public static decimal? Variacao(decimal atual, decimal anterior) =>
            anterior == 0 ? null : Math.Round((atual - anterior) / anterior * 100, 0);
    }

    public class VendaDiaViewModel
    {
        public DateOnly Dia { get; set; }
        public int Pedidos { get; set; }
        public decimal Valor { get; set; }
    }

    public class MaisVendidoViewModel
    {
        public int ProdutoId { get; set; }
        public string Jogo { get; set; } = string.Empty;
        public string Plataforma { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;
        public int Unidades { get; set; }
        public decimal Receita { get; set; }
    }
}
