using Clouud.Web.Models;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Bloqueio da conta depois de várias senhas erradas seguidas (padrão: 5 erros → 15 minutos).
    /// Complementa o limite por IP do rate limiter: pega também quem tenta a mesma conta de vários IPs.
    /// </summary>
    public class ProtecaoLogin
    {
        public ProtecaoLogin(IConfiguration configuracao)
        {
            TentativasAntesDoBloqueio = Math.Max(1, configuracao.GetValue("Seguranca:Login:TentativasAntesDoBloqueio", 5));
            Bloqueio = TimeSpan.FromMinutes(Math.Max(1, configuracao.GetValue("Seguranca:Login:MinutosDeBloqueio", 15)));
        }

        public int TentativasAntesDoBloqueio { get; }
        public TimeSpan Bloqueio { get; }

        public bool EstaBloqueado(Usuario usuario, out TimeSpan falta)
        {
            falta = usuario.BloqueadoAte.HasValue ? usuario.BloqueadoAte.Value - DateTime.UtcNow : TimeSpan.Zero;
            return falta > TimeSpan.Zero;
        }

        /// <summary>Conta a senha errada. Devolve true se a conta acabou de ser bloqueada. Quem chama salva.</summary>
        public bool RegistrarFalha(Usuario usuario)
        {
            usuario.FalhasLogin++;
            if (usuario.FalhasLogin < TentativasAntesDoBloqueio)
            {
                return false;
            }
            usuario.FalhasLogin = 0;
            usuario.BloqueadoAte = DateTime.UtcNow + Bloqueio;
            return true;
        }

        public static void Liberar(Usuario usuario)
        {
            usuario.FalhasLogin = 0;
            usuario.BloqueadoAte = null;
        }
    }
}
