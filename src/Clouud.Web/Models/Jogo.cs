using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Informações do jogo (título, descrição, capa). O que é vendido é o <see cref="Produto"/>:
    /// o mesmo jogo pode ser vendido em várias plataformas e edições, cada uma com seu preço.
    /// </summary>
    [Table("Jogos")]
    [Index(nameof(Slug), IsUnique = true)]
    public class Jogo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Título obrigatório")]
        [StringLength(150)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Título simplificado para URLs (ex.: "elden-ring").</summary>
        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(4000)]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Lançamento")]
        [DataType(DataType.Date)]
        public DateOnly? DataLancamento { get; set; }

        /// <summary>L, 10, 12, 14, 16 ou 18.</summary>
        [StringLength(2)]
        [Display(Name = "Classificação indicativa")]
        public string? ClassificacaoIndicativa { get; set; }

        /// <summary>Nome do arquivo da capa em wwwroot/uploads.</summary>
        [StringLength(300)]
        public string? Capa { get; set; }

        public int? DesenvolvedoraId { get; set; }

        [ForeignKey(nameof(DesenvolvedoraId))]
        public Empresa? Desenvolvedora { get; set; }

        public int? PublicadoraId { get; set; }

        [ForeignKey(nameof(PublicadoraId))]
        public Empresa? Publicadora { get; set; }

        /// <summary>Aparece primeiro na vitrine.</summary>
        public bool Destaque { get; set; }

        /// <summary>Jogos inativos somem da loja, mas continuam nos pedidos antigos.</summary>
        public bool Ativo { get; set; } = true;

        public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();

        public static readonly string[] Classificacoes = ["L", "10", "12", "14", "16", "18"];
    }
}
