using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Código")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Perfil do Usuário")]
        public PerfilUsuario Perfil { get; set; }
       
        
        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100)]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válido")]
        public string Senha { get; set; } = string.Empty;

        [DataType(DataType.ImageUrl)]
        public string? Foto { get; set; }

        [ValidateNever]
        public virtual Cliente Cliente { get; set; } = null!;

    }
    public enum PerfilUsuario 
    { 
     Cliente = 0,    
     Admin = 1
    }
}
