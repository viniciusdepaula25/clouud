using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoFinal_LojadeJogos.Data
{
    [Table("Jogos")]
    public class Jogo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        public string Nome { get; set; }

        [StringLength(50)]
        public string Desenvolvedora { get; set; }

        [StringLength(50)]
        public string Plataforma { get; set; }

        [StringLength(20)]
        public string Categoria { get; set; }

        [DataType(DataType.Currency)]
        public double Valorj {  get; set; }

        [DataType(DataType.ImageUrl)]
        public string? Foto { get; set; }

        public Jogo() 
        {
          Valorj = 0;
        }
    
    
    
    
    
    
    
    
    
    }
}
