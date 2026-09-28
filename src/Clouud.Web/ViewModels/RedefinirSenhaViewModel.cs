using System.ComponentModel.DataAnnotations;

namespace Clouud.Web.ViewModels
{
    public class EsqueciSenhaViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail da sua conta")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
    }

    public class RedefinirSenhaViewModel
    {
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
