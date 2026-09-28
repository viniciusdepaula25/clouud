using System.ComponentModel.DataAnnotations;
using Clouud.Web.Infraestrutura;

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
        [StringLength(100, ErrorMessage = "A senha pode ter no máximo 100 caracteres")]
        [SenhaForte]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
