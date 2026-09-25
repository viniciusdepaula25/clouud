using System.ComponentModel.DataAnnotations;

namespace ProjetoFinal_LojadeJogos.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100)]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válida")]
        public string Senha { get; set; }

    }
}
