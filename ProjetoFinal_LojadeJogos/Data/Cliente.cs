using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoFinal_LojadeJogos.Data
{
    [Table("Clientes")]
    public class Cliente
    {
        [ForeignKey("Usuario")]
        [Display(Name ="Usuário")]
        public int Id { get; set; }
        public virtual Usuario Usuario { get; set; }

        [StringLength(30)]
        public string Nome { get; set; }

        public ICollection<PedidoJ> pedidoJs { get; set; }
    
    
    
    
    }
}
