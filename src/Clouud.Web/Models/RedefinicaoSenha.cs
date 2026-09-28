using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Pedido de "esqueci minha senha". O código do link não fica no banco, só o hash SHA-256 dele:
    /// quem ler o banco não consegue montar um link válido. Vale 1 hora e uma vez só.
    /// </summary>
    [Table("redefinicoes_senha")]
    [Index(nameof(TokenHash), IsUnique = true)]
    public class RedefinicaoSenha
    {
        public static readonly TimeSpan Validade = TimeSpan.FromHours(1);

        /// <summary>Máximo de pedidos por conta em uma hora (evita lotar a caixa de alguém).</summary>
        public const int PedidosPorHora = 3;

        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        [Required, StringLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

        public DateTime ExpiraEm { get; set; }

        public DateTime? UsadaEm { get; set; }
    }
}
