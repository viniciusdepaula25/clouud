using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Desenvolvedora ou publicadora de jogos.</summary>
    [Table("Empresas")]
    [Index(nameof(Slug), IsUnique = true)]
    public class Empresa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;
    }
}
