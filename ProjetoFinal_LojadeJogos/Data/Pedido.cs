using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoFinal_LojadeJogos.Data
{
    [Table("Pedidos")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Usuario")]
        [Display(Name = "Usuário")]
       public int Id_Usuario { get; set; }

        [DataType(DataType.Currency)]
        public double Valor {  get; set; }
        
        public ICollection<PedidoJ> PedidoJs { get; set; }

        public Pedido()
        { 
          Valor = 0;
        }
    }
}
