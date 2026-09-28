using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    public enum TipoCupom
    {
        /// <summary>Desconto em % do subtotal.</summary>
        Percentual,
        /// <summary>Desconto de um valor em reais (limitado ao subtotal).</summary>
        ValorFixo
    }

    /// <summary>Cupom de desconto aplicado no carrinho. Um pedido usa no máximo um cupom.</summary>
    [Table("cupons")]
    [Index(nameof(Codigo), IsUnique = true)]
    public class Cupom : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Guardado em maiúsculas; o cliente pode digitar de qualquer jeito.</summary>
        [Required(ErrorMessage = "Informe o código")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "O código deve ter de 3 a 30 caracteres")]
        [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use só letras, números e hífen")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Descrição (só para o admin)")]
        public string? Descricao { get; set; }

        public TipoCupom Tipo { get; set; } = TipoCupom.Percentual;

        /// <summary>% (1 a 100) ou valor em reais, conforme o tipo.</summary>
        [Required(ErrorMessage = "Informe o valor do desconto")]
        [Range(0.01, 99999999, ErrorMessage = "Valor inválido")]
        [Column(TypeName = "numeric(10,2)")]
        [Display(Name = "Desconto")]
        public decimal Valor { get; set; }

        [Range(0, 99999999, ErrorMessage = "Valor inválido")]
        [Column(TypeName = "numeric(10,2)")]
        [Display(Name = "Pedido mínimo")]
        public decimal? PedidoMinimo { get; set; }

        [Display(Name = "Válido de")]
        [DataType(DataType.Date)]
        public DateOnly? ValidoDe { get; set; }

        [Display(Name = "Válido até")]
        [DataType(DataType.Date)]
        public DateOnly? ValidoAte { get; set; }

        /// <summary>Quantos pedidos podem usar o cupom no total. Nulo = sem limite.</summary>
        [Range(1, 1000000, ErrorMessage = "Informe um número a partir de 1")]
        [Display(Name = "Limite de usos (total)")]
        public int? LimiteUsos { get; set; }

        /// <summary>Quantas vezes o mesmo cliente pode usar. Nulo = sem limite.</summary>
        [Range(1, 1000, ErrorMessage = "Informe um número a partir de 1")]
        [Display(Name = "Limite por cliente")]
        public int? LimitePorCliente { get; set; } = 1;

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

        /// <summary>Desconto sobre o subtotal, arredondado em centavos e nunca maior que o subtotal.</summary>
        public decimal CalcularDesconto(decimal subtotal)
        {
            var desconto = Tipo == TipoCupom.Percentual
                ? Math.Round(subtotal * Valor / 100, 2, MidpointRounding.AwayFromZero)
                : Valor;
            return Math.Min(desconto, subtotal);
        }

        /// <summary>"12,5%" ou "R$ 20,00".</summary>
        public string DescreverDesconto() =>
            Tipo == TipoCupom.Percentual ? $"{Infraestrutura.Dinheiro.Numero(Valor)}%" : Valor.ToString("C");

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Tipo == TipoCupom.Percentual && Valor > 100)
            {
                yield return new ValidationResult("O desconto em % vai até 100", [nameof(Valor)]);
            }
            if (ValidoDe.HasValue && ValidoAte.HasValue && ValidoAte < ValidoDe)
            {
                yield return new ValidationResult("A data final deve ser depois da inicial", [nameof(ValidoAte)]);
            }
        }
    }
}
