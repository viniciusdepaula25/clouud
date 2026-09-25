using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    public class PedidoJ
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int IdPedido { get; set; }
        //Relacionamento FK
        [ForeignKey("IdPedido")]
        public Pedido Servico { get; set; }

        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario usuario { get; set; }

        public int IdJogo { get; set; }
        [ForeignKey("IdJogo")]
        public Jogo jogo { get; set; }

        public int  Quantidade { get; set; }

        [Display(Name = "Valor unitário")]
        public double ValorUni { get; set; }

        [Display(Name = "Valor Total")]
        public double ValorTotal { get; set; }

        public ICollection<Jogo> jogos { get; set; }

        public PedidoJ() 
        { 
         Quantidade = 0;
         ValorUni = 0;
         ValorTotal = 0;
        }
    
    
    
    
    
    
    
    
    
    }
}
