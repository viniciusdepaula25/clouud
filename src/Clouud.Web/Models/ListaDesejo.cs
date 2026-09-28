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

        // Situação do jogo na última conferência dos avisos (serviço AvisosListaDesejos).
        // Serve para mandar o e-mail só quando algo muda: voltou ao estoque ou baixou o preço da promoção.

        /// <summary>Quando o serviço de avisos conferiu este jogo pela primeira vez (nulo: ainda não conferiu).</summary>
        public DateTime? AvisoConferidoEm { get; set; }

        /// <summary>Tinha chave disponível na última conferência?</summary>
        public bool AvisoDisponivel { get; set; }

        /// <summary>Menor preço promocional na última conferência (nulo: sem promoção).</summary>
        [Column(TypeName = "numeric(10,2)")]
        public decimal? AvisoPrecoPromocao { get; set; }
    }
}
