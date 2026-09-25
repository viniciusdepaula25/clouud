using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    /// <summary>Situação do pedido. Gravada no banco como texto ("Pago", "Cancelado"...).</summary>
    public enum StatusPedido
    {
        /// <summary>Criado; as chaves ficam reservadas até o prazo de pagamento.</summary>
        AguardandoPagamento,
        /// <summary>Pagamento aprovado; as chaves foram entregues.</summary>
        Pago,
        /// <summary>Cancelado antes do pagamento (pelo cliente, pelo admin ou por prazo vencido).</summary>
        Cancelado,
        /// <summary>Pago e depois devolvido; as chaves ficam inativas.</summary>
        Reembolsado
    }

    public static class StatusPedidoExtensions
    {
        public static string Descricao(this StatusPedido status) => status switch
        {
            StatusPedido.AguardandoPagamento => "Aguardando pagamento",
            StatusPedido.Pago => "Pago",
            StatusPedido.Cancelado => "Cancelado",
            StatusPedido.Reembolsado => "Reembolsado",
            _ => status.ToString()
        };

        /// <summary>Cor do selo (classe do Bootstrap) de cada situação.</summary>
        public static string Cor(this StatusPedido status) => status switch
        {
            StatusPedido.AguardandoPagamento => "bg-warning text-dark",
            StatusPedido.Pago => "bg-success",
            StatusPedido.Reembolsado => "bg-info text-dark",
            _ => "bg-secondary"
        };
    }

    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Usuário")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        public StatusPedido Status { get; set; } = StatusPedido.AguardandoPagamento;

        [DataType(DataType.Currency)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Total { get; set; }

        [Display(Name = "Criado em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        /// <summary>Prazo para pagar. Depois dele o pedido é cancelado e as chaves voltam ao estoque.</summary>
        [Display(Name = "Pagar até")]
        public DateTime? PagarAte { get; set; }

        [Display(Name = "Pago em")]
        public DateTime? PagoEm { get; set; }

        public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
        public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
    }
}
