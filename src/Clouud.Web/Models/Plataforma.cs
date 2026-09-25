using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Onde a chave é ativada: Steam, Epic Games, Ubisoft Connect, Battle.net...</summary>
    [Table("plataformas")]
    [Index(nameof(Slug), IsUnique = true)]
    public class Plataforma
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Nome simplificado usado em URLs e filtros (ex.: "epic-games").</summary>
        [StringLength(60)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>Passo a passo mostrado ao cliente junto da chave comprada.</summary>
        [StringLength(1000)]
        [Display(Name = "Instruções de ativação")]
        public string? InstrucoesAtivacao { get; set; }

        public bool Ativa { get; set; } = true;

        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}
