using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Clouud.Web.ViewModels
{
    /// <summary>Formulário de produto (jogo + plataforma + edição, com preço).</summary>
    public class ProdutoFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Escolha o jogo")]
        [Display(Name = "Jogo")]
        public int? JogoId { get; set; }

        [Required(ErrorMessage = "Escolha a plataforma")]
        [Display(Name = "Plataforma")]
        public int? PlataformaId { get; set; }

        [Required(ErrorMessage = "Informe a edição")]
        [StringLength(60)]
        [Display(Name = "Edição")]
        public string Edicao { get; set; } = "Standard";

        [Required(ErrorMessage = "Informe a região")]
        [StringLength(40)]
        [Display(Name = "Região de ativação")]
        public string Regiao { get; set; } = "Global";

        [Required(ErrorMessage = "Informe o preço")]
        [Range(0, 99999999, ErrorMessage = "Preço inválido")]
        [Display(Name = "Preço")]
        public decimal? Preco { get; set; }

        [Range(0, 99999999, ErrorMessage = "Preço inválido")]
        [Display(Name = "Preço promocional")]
        public decimal? PrecoPromocional { get; set; }

        [Display(Name = "Promoção até")]
        [DataType(DataType.Date)]
        public DateOnly? PromocaoAte { get; set; }

        [Display(Name = "Ativo (à venda)")]
        public bool Ativo { get; set; } = true;

        public List<SelectListItem> OpcoesJogos { get; set; } = new();
        public List<SelectListItem> OpcoesPlataformas { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PrecoPromocional.HasValue && Preco.HasValue && PrecoPromocional >= Preco)
            {
                yield return new ValidationResult("O preço promocional deve ser menor que o preço normal",
                    [nameof(PrecoPromocional)]);
            }
        }
    }
}
