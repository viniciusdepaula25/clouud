using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// O que a loja vende: um jogo, em uma plataforma, em uma edição.
    /// Ex.: "Elden Ring — Steam — Standard". Tem preço próprio e estoque de chaves.
    /// </summary>
    [Table("Produtos")]
    [Index(nameof(JogoId), nameof(PlataformaId), nameof(Edicao), IsUnique = true)]
    public class Produto
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Jogo")]
        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo Jogo { get; set; } = null!;

        [Display(Name = "Plataforma")]
        public int PlataformaId { get; set; }

        [ForeignKey(nameof(PlataformaId))]
        public Plataforma Plataforma { get; set; } = null!;

        [StringLength(60)]
        [Display(Name = "Edição")]
        public string Edicao { get; set; } = "Standard";

        /// <summary>Onde a chave pode ser ativada (Global, Brasil, América Latina...).</summary>
        [StringLength(40)]
        [Display(Name = "Região")]
        public string Regiao { get; set; } = "Global";

        [Column(TypeName = "numeric(10,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Preço")]
        public decimal Preco { get; set; }

        /// <summary>Preço com desconto. Nulo quando não está em promoção.</summary>
        [Column(TypeName = "numeric(10,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Preço promocional")]
        public decimal? PrecoPromocional { get; set; }

        /// <summary>Último dia da promoção. Nulo = sem data para acabar.</summary>
        [Display(Name = "Promoção até")]
        [DataType(DataType.Date)]
        public DateOnly? PromocaoAte { get; set; }

        public bool Ativo { get; set; } = true;

        /// <summary>Estoque de chaves do produto.</summary>
        public ICollection<Chave> Chaves { get; set; } = new List<Chave>();

        public bool EmPromocao(DateOnly hoje) =>
            PrecoPromocional.HasValue && PrecoPromocional < Preco && (PromocaoAte == null || PromocaoAte >= hoje);

        public decimal PrecoAtual(DateOnly hoje) => EmPromocao(hoje) ? PrecoPromocional!.Value : Preco;
    }
}
