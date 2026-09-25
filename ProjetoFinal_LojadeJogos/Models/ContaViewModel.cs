using ProjetoFinal_LojadeJogos.Data;
using System.ComponentModel.DataAnnotations;


namespace ProjetoFinal_LojadeJogos.Models
{
    public class ContaViewModel
    {
        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required(ErrorMessage = "E-mail obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100)]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válida")]
        public string Senha { get; set; }

        [Display(Name = "Perfil do usuário")]
        public PerfilUsuario PerfilUsuario { get; set;}

    }
}


    

