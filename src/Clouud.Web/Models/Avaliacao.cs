using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Nota (1 a 5) e comentário de um cliente sobre um jogo. Um por cliente e jogo (pode ser editado),
    /// e só de quem comprou o jogo.
    /// </summary>
    [Table("avaliacoes")]
    [PrimaryKey(nameof(UsuarioId), nameof(JogoId))]
    [Index(nameof(JogoId), nameof(CriadaEm))]
    public class Avaliacao
    {
        public const int TamanhoComentario = 1000;

        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo Jogo { get; set; } = null!;

        [Range(1, 5, ErrorMessage = "Escolha de 1 a 5 estrelas")]
        public int Nota { get; set; }

        [StringLength(TamanhoComentario, ErrorMessage = "O comentário pode ter até 1000 caracteres")]
        [Display(Name = "Comentário")]
        public string? Comentario { get; set; }

        public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

        public DateTime? AtualizadaEm { get; set; }
    }
}
