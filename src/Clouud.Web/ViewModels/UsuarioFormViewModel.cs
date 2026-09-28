using System.ComponentModel.DataAnnotations;
using Clouud.Web.Infraestrutura;
using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>
    /// Formulário de inclusão/alteração de usuário no painel admin.
    /// Separado da entidade Usuario para nunca expor o hash da senha na tela.
    /// </summary>
    public class UsuarioFormViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60, ErrorMessage = "O nome pode ter no máximo 60 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Perfil do usuário")]
        public PerfilUsuario Perfil { get; set; }

        /// <summary>Obrigatória na inclusão. Na alteração, em branco = mantém a senha atual.</summary>
        [StringLength(100, ErrorMessage = "A senha pode ter no máximo 100 caracteres")]
        [SenhaForte]
        [DataType(DataType.Password)]
        public string? Senha { get; set; }
    }
}
