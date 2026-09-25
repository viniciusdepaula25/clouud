using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Jogo que o cliente salvou para acompanhar (preço, promoção, volta ao estoque).
    /// A lista é por jogo, não por produto: vale para qualquer plataforma em que o jogo é vendido.
    /// </summary>
    [Table("lista_desejos")]
    [PrimaryKey(nameof(UsuarioId), nameof(JogoId))]
    public class ListaDesejo
    {
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo Jogo { get; set; } = null!;

        [Display(Name = "Adicionado em")]
        public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;
    }
}
