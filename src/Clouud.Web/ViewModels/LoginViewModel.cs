using System.ComponentModel.DataAnnotations;

namespace Clouud.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100)]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válida")]
        public string Senha { get; set; } = string.Empty;

    }
}
