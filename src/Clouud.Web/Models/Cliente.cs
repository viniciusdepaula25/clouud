using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    [Table("Clientes")]
    public class Cliente
    {
        [ForeignKey("Usuario")]
        [Display(Name ="Usuário")]
        public int Id { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        [StringLength(30)]
        public string Nome { get; set; } = string.Empty;

        public ICollection<PedidoJ> pedidoJs { get; set; } = new List<PedidoJ>();
    
    
    
    
    }
}
