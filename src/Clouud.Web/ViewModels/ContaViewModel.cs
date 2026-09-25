using System.ComponentModel.DataAnnotations;


namespace Clouud.Web.ViewModels
{
    public class ContaViewModel
    {
        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60, ErrorMessage = "O nome pode ter no máximo 60 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válida")]
        public string Senha { get; set; } = string.Empty;

    }
}


    

