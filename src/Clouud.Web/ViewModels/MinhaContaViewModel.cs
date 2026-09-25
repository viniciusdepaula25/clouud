using System.ComponentModel.DataAnnotations;

namespace Clouud.Web.ViewModels
{
    /// <summary>Dados que o próprio usuário pode alterar na tela "Minha conta".</summary>
    public class MinhaContaViewModel
    {
        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60, ErrorMessage = "O nome pode ter no máximo 60 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Exigida para trocar a senha ou o e-mail.</summary>
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string? SenhaAtual { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string? NovaSenha { get; set; }

        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string? ConfirmarNovaSenha { get; set; }
    }
}
