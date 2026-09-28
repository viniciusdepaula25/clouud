using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clouud.Web.Models
{
    [Table("usuarios")]
    [Index(nameof(Email), IsUnique = true)] // não permite dois usuários com o mesmo e-mail
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Código")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Nome obrigatório")]
        [StringLength(60)]
        [Display(Name = "Nome")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email obrigatório")]
        [StringLength(100)]
        [DataType(DataType.EmailAddress, ErrorMessage = "Informe um e-mail válido")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Perfil do Usuário")]
        public PerfilUsuario Perfil { get; set; }
       
        
        [Required(ErrorMessage = "Senha obrigatória")]
        [StringLength(100)]
        [DataType(DataType.Password, ErrorMessage = "Informe uma senha válido")]
        public string Senha { get; set; } = string.Empty;

        [DataType(DataType.ImageUrl)]
        public string? Foto { get; set; }

        /// <summary>Quando o usuário clicou no link de confirmação do e-mail (nulo: ainda não confirmou).</summary>
        [Display(Name = "E-mail confirmado em")]
        public DateTime? EmailConfirmadoEm { get; set; }

        /// <summary>Quer receber por e-mail os avisos da lista de desejos (promoção e volta ao estoque)?</summary>
        [Display(Name = "Receber avisos da lista de desejos")]
        public bool ReceberAvisos { get; set; } = true;

        /// <summary>
        /// "Selo de segurança": valor aleatório gravado também no cookie de login. Trocar o selo (nova senha,
        /// novo e-mail, mudança de perfil, "sair de todos os aparelhos") derruba todas as sessões abertas.
        /// </summary>
        [StringLength(32)]
        public string SeloSeguranca { get; set; } = NovoSelo();

        public static string NovoSelo() => Guid.NewGuid().ToString("N");

        public void TrocarSelo() => SeloSeguranca = NovoSelo();

        /// <summary>Senhas erradas seguidas (volta a zero no login certo ou quando a conta é bloqueada).</summary>
        public int FalhasLogin { get; set; }

        /// <summary>Conta bloqueada para login até este momento, depois de muitas senhas erradas.</summary>
        public DateTime? BloqueadoAte { get; set; }

        [ValidateNever]
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    }
    public enum PerfilUsuario 
    { 
     Cliente = 0,    
     Admin = 1
    }
}
