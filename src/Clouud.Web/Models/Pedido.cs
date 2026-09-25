using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    [Table("Pedidos")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Usuário")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        [DataType(DataType.Currency)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Valor { get; set; }

        public ICollection<PedidoJogo> Itens { get; set; } = new List<PedidoJogo>();
    }
}
