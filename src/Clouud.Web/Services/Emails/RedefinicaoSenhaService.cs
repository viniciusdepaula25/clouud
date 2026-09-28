using System.Security.Cryptography;
using System.Text;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services.Emails
{
    /// <summary>"Esqueci minha senha": gera o link por e-mail e troca a senha quando o link é usado.</summary>
    public class RedefinicaoSenhaService
    {
        private readonly BancoDados bancoDados;
        private readonly FilaEmails fila;
        private readonly LinksLoja links;
        private readonly SenhaService senhas;

        public RedefinicaoSenhaService(BancoDados bancoDados, FilaEmails fila, LinksLoja links, SenhaService senhas)
        {
            this.bancoDados = bancoDados;
            this.fila = fila;
            this.links = links;
            this.senhas = senhas;
        }

        /// <summary>
        /// Manda o link para o e-mail, se existir uma conta com ele. Quem pediu nunca fica sabendo se o e-mail
        /// tem conta (a tela responde sempre igual), para ninguém descobrir quem é cliente da loja.
        /// </summary>
        public async Task SolicitarAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();
            var usuario = bancoDados.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                // Monta um e-mail igual (e joga fora) para a resposta levar o mesmo tempo: medir o tempo não
                // revela se o e-mail tem conta
                await fila.RenderizarAsync("RedefinirSenha", new EmailRedefinirSenhaViewModel("Cliente",
                    links.Absoluto($"/conta/redefinir-senha?token={WebEncoders(RandomNumberGenerator.GetBytes(32))}")));
                return;
            }
            var umaHoraAtras = DateTime.UtcNow.AddHours(-1);
            if (bancoDados.RedefinicoesSenha.Count(r => r.UsuarioId == usuario.ID && r.CriadaEm > umaHoraAtras)
                >= RedefinicaoSenha.PedidosPorHora)
            {
                return;
            }

            var token = WebEncoders(RandomNumberGenerator.GetBytes(32));
            bancoDados.RedefinicoesSenha.Add(new RedefinicaoSenha
            {
                UsuarioId = usuario.ID,
                TokenHash = Hash(token),
                ExpiraEm = DateTime.UtcNow + RedefinicaoSenha.Validade
            });
            await fila.AdicionarAsync(TipoEmail.RedefinirSenha, usuario, "Redefinir sua senha na CLOUUD", "RedefinirSenha",
                new EmailRedefinirSenhaViewModel(usuario.Name, links.Absoluto($"/conta/redefinir-senha?token={token}")));
            bancoDados.SaveChanges();
        }

        /// <summary>Pedido válido do link (não usado e dentro da validade), ou nulo.</summary>
        public RedefinicaoSenha? Validar(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
            {
                return null;
            }
            var hash = Hash(token);
            var agora = DateTime.UtcNow;
            return bancoDados.RedefinicoesSenha
                .Include(r => r.Usuario)
                .FirstOrDefault(r => r.TokenHash == hash && r.UsadaEm == null && r.ExpiraEm > agora);
        }

        /// <summary>Troca a senha. O link deixa de valer, assim como os outros links pedidos antes.</summary>
        public async Task<bool> RedefinirAsync(string? token, string novaSenha)
        {
            var pedido = Validar(token);
            if (pedido == null)
            {
                return false;
            }
            var usuario = pedido.Usuario;
            usuario.Senha = senhas.GerarHash(usuario, novaSenha);
            ProtecaoLogin.Liberar(usuario); // quem provou ser dono do e-mail pode entrar de novo
            usuario.TrocarSelo();           // e quem estava com a conta aberta em outro aparelho sai

            var agora = DateTime.UtcNow;
            foreach (var aberto in bancoDados.RedefinicoesSenha.Where(r => r.UsuarioId == usuario.ID && r.UsadaEm == null))
            {
                aberto.UsadaEm = agora;
            }
            // Quem clicou no link recebeu o e-mail: o endereço está confirmado
            usuario.EmailConfirmadoEm ??= agora;

            await fila.AdicionarAsync(TipoEmail.SenhaAlterada, usuario, "Sua senha da CLOUUD foi alterada", "SenhaAlterada",
                new EmailSenhaAlteradaViewModel(usuario.Name, "pelo link de \"esqueci minha senha\"", links.Absoluto("/conta/esqueci-senha")));
            bancoDados.SaveChanges();
            return true;
        }

        private static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        /// <summary>Base64 que pode ir na URL sem escapar (sem +, / e =).</summary>
        private static string WebEncoders(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public record EmailRedefinirSenhaViewModel(string Nome, string Link);

    public record EmailSenhaAlteradaViewModel(string Nome, string Como, string LinkEsqueciSenha);

    public record EmailAlteradoViewModel(string Nome, string NovoEmailMascarado, string LinkEsqueciSenha);
}
