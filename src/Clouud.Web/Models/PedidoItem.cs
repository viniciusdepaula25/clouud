using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Item de um pedido (o Pedido_Jogo do diagrama, agora apontando para o produto).
    /// Guarda o preço da hora da compra: se o preço do produto mudar depois, o pedido continua certo.
    /// </summary>
    [Table("pedido_itens")]
    [Index(nameof(PedidoId), nameof(ProdutoId), IsUnique = true)]
    public class PedidoItem
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido Pedido { get; set; } = null!;

        public int ProdutoId { get; set; }

        [ForeignKey(nameof(ProdutoId))]
        public Produto Produto { get; set; } = null!;

        public int Quantidade { get; set; }

        [Display(Name = "Preço unitário")]
        [Column(TypeName = "numeric(10,2)")]
        public decimal PrecoUnitario { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal Subtotal { get; set; }

        /// <summary>Chaves reservadas ou entregues neste item.</summary>
        public ICollection<Chave> Chaves { get; set; } = new List<Chave>();
    }
}
