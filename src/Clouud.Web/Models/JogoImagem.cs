using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Imagem da galeria de um jogo (telas do jogo), mostrada na página do jogo na ordem definida pelo admin.</summary>
    [Table("jogo_imagens")]
    [Index(nameof(JogoId), nameof(Ordem))]
    public class JogoImagem
    {
        public const int MaximoPorJogo = 12;
        public const long TamanhoMaximo = 5 * 1024 * 1024;
        public const int TamanhoLegenda = 150;

        [Key]
        public int Id { get; set; }

        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo Jogo { get; set; } = null!;

        /// <summary>Caminho dentro de wwwroot/uploads (ex.: "galeria/abc123.jpg").</summary>
        [Required]
        [StringLength(300)]
        public string Arquivo { get; set; } = string.Empty;

        [StringLength(TamanhoLegenda)]
        public string? Legenda { get; set; }

        public int Ordem { get; set; }

        public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    }
}
