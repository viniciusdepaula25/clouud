using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Gênero do jogo (Ação, RPG, Luta...). Um jogo pode ter várias categorias.</summary>
    [Table("Categorias")]
    [Index(nameof(Slug), IsUnique = true)]
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(60)]
        public string Slug { get; set; } = string.Empty;

        public ICollection<Jogo> Jogos { get; set; } = new List<Jogo>();
    }
}
