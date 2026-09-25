using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Produto que o cliente separou antes de pagar. O mesmo produto aparece uma vez; a quantidade aumenta.</summary>
    [Table("CarrinhoItens")]
    [Index(nameof(UsuarioId), nameof(ProdutoId), IsUnique = true)]
    public class CarrinhoItem
    {
        public const int QuantidadeMaxima = 10;

        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;

        public int ProdutoId { get; set; }

        [ForeignKey(nameof(ProdutoId))]
        public Produto Produto { get; set; } = null!;

        public int Quantidade { get; set; } = 1;

        public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;
    }
}
