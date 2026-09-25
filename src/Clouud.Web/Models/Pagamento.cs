using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    public enum MetodoPagamento
    {
        Pix,
        Cartao,
        Boleto
    }

    public enum StatusPagamento
    {
        Pendente,
        Aprovado,
        Recusado
    }

    public static class PagamentoExtensions
    {
        public static string Descricao(this MetodoPagamento metodo) => metodo switch
        {
            MetodoPagamento.Pix => "Pix",
            MetodoPagamento.Cartao => "Cartão de crédito",
            MetodoPagamento.Boleto => "Boleto",
            _ => metodo.ToString()
        };
    }

    /// <summary>
    /// Uma tentativa de pagamento do pedido. Por enquanto o pagamento é simulado: não há integração
    /// com operadora, e o código da transação é gerado pela própria loja.
    /// </summary>
    [Table("Pagamentos")]
    public class Pagamento
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido Pedido { get; set; } = null!;

        [Display(Name = "Forma de pagamento")]
        public MetodoPagamento Metodo { get; set; }

        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

        [Column(TypeName = "numeric(10,2)")]
        public decimal Valor { get; set; }

        [StringLength(60)]
        [Display(Name = "Código da transação")]
        public string? CodigoTransacao { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? ConfirmadoEm { get; set; }
    }
}
