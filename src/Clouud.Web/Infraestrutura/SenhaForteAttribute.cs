using System.ComponentModel.DataAnnotations;
using Clouud.Web.Services;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Senha com pelo menos <see cref="SenhaService.TamanhoMinimo"/> caracteres, que não seja uma das
    /// senhas mais usadas (as primeiras que um robô tenta) e que não tenha só um caractere repetido.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SenhaForteAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> Comuns = new(StringComparer.OrdinalIgnoreCase)
        {
            "12345678", "123456789", "1234567890", "12341234", "87654321", "11223344", "123123123", "00000000",
            "password", "password1", "passw0rd", "qwerty123", "qwertyuiop", "abcd1234", "abc12345", "iloveyou",
            "senha123", "senha1234", "senha@123", "mudar123", "admin123", "admin@123", "administrador",
            "brasil123", "flamengo", "corinthians", "palmeiras", "saopaulo", "gremio123", "123mudar",
            "clouud123", "clouud2026", "gamer123", "steam123"
        };

        public override bool IsValid(object? value)
        {
            if (value is not string senha || senha.Length == 0)
            {
                return true; // [Required] cuida do vazio
            }
            if (senha.Length < SenhaService.TamanhoMinimo)
            {
                ErrorMessage = $"A senha deve ter pelo menos {SenhaService.TamanhoMinimo} caracteres";
                return false;
            }
            if (Comuns.Contains(senha) || senha.Distinct().Count() == 1)
            {
                ErrorMessage = "Esta senha é muito comum e fácil de adivinhar. Escolha outra";
                return false;
            }
            return true;
        }
    }
}
