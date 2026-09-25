using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Item de um pedido (tabela associativa Pedido_Jogo do diagrama).
    /// A chave primária é o par (pedido, jogo): o mesmo jogo não se repete no pedido,
    /// a quantidade é que aumenta.
    /// </summary>
    [Table("PedidoJogos")]
    [PrimaryKey(nameof(PedidoId), nameof(JogoId))]
    public class PedidoJogo
    {
        public int PedidoId { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido Pedido { get; set; } = null!;

        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo Jogo { get; set; } = null!;

        public int Quantidade { get; set; }

        [Display(Name = "Valor unitário")]
        [Column(TypeName = "numeric(10,2)")]
        public decimal ValorUnitario { get; set; }

        [Display(Name = "Valor total")]
        [Column(TypeName = "numeric(10,2)")]
        public decimal ValorTotal { get; set; }
    }
}
