using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    [Table("Jogos")]
    public class Jogo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        public string Desenvolvedora { get; set; } = string.Empty;

        [StringLength(50)]
        public string Plataforma { get; set; } = string.Empty;

        [StringLength(20)]
        public string Categoria { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Valor { get; set; }

        [DataType(DataType.ImageUrl)]
        public string? Foto { get; set; }

    }
}
