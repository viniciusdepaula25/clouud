using System.ComponentModel.DataAnnotations;
using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Formulário de inclusão/alteração de jogo no admin.</summary>
    public class JogoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Título obrigatório")]
        [StringLength(150)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(4000)]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Lançamento")]
        [DataType(DataType.Date)]
        public DateOnly? DataLancamento { get; set; }

        [Display(Name = "Classificação indicativa")]
        public string? ClassificacaoIndicativa { get; set; }

        /// <summary>Nome digitado; se a empresa não existir, é criada ao salvar.</summary>
        [StringLength(100)]
        public string? Desenvolvedora { get; set; }

        [StringLength(100)]
        public string? Publicadora { get; set; }

        [Display(Name = "Categorias")]
        public List<int> CategoriaIds { get; set; } = new();

        [Display(Name = "Destaque (aparece primeiro na vitrine)")]
        public bool Destaque { get; set; }

        [Display(Name = "Ativo (aparece na loja)")]
        public bool Ativo { get; set; } = true;

        /// <summary>Capa atual (nome do arquivo em uploads), só para exibição.</summary>
        public string? CapaAtual { get; set; }

        // Opções dos campos
        public List<Categoria> OpcoesCategorias { get; set; } = new();
        public List<string> OpcoesEmpresas { get; set; } = new();
    }
}
