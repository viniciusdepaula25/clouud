using System.ComponentModel.DataAnnotations;
using Clouud.Web.Infraestrutura;


namespace Clouud.Web.ViewModels
{
    public class ContaViewModel
    {
        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60, ErrorMessage = "O nome pode ter no máximo 60 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100, ErrorMessage = "A senha pode ter no máximo 100 caracteres")]
        [SenhaForte]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válida")]
        public string Senha { get; set; } = string.Empty;

    }
}


    

