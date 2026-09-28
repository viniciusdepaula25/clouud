using System.Security.Cryptography;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.DataProtection;

namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Confirmação do e-mail do cadastro. O link leva um código assinado pela aplicação (Data Protection)
    /// com o id e o e-mail do usuário e validade de 7 dias; não precisa guardar nada no banco.
    /// Se o usuário trocar de e-mail, os links antigos deixam de valer.
    /// </summary>
    public class ConfirmacaoEmail
    {
        public static readonly TimeSpan Validade = TimeSpan.FromDays(7);

        /// <summary>Tempo mínimo entre dois pedidos de reenvio do e-mail de confirmação.</summary>
        public static readonly TimeSpan IntervaloReenvio = TimeSpan.FromMinutes(2);

        private readonly ITimeLimitedDataProtector protetor;
        private readonly BancoDados bancoDados;
        private readonly FilaEmails fila;
        private readonly LinksLoja links;

        public ConfirmacaoEmail(IDataProtectionProvider protecao, BancoDados bancoDados, FilaEmails fila, LinksLoja links)
        {
            protetor = protecao.CreateProtector("Clouud.ConfirmarEmail").ToTimeLimitedDataProtector();
            this.bancoDados = bancoDados;
            this.fila = fila;
            this.links = links;
        }

        public string GerarLink(Usuario usuario)
        {
            var codigo = protetor.Protect($"{usuario.ID}|{usuario.Email}", Validade);
            return links.Absoluto($"/conta/confirmar-email?codigo={Uri.EscapeDataString(codigo)}");
        }

        /// <summary>Confirma o e-mail do código. Devolve o usuário confirmado, ou nulo se o link for inválido ou vencido.</summary>
        public Usuario? Confirmar(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }
            string conteudo;
            try
            {
                conteudo = protetor.Unprotect(codigo);
            }
            catch (CryptographicException)
            {
                return null; // alterado, vencido ou de outra instalação
            }

            var partes = conteudo.Split('|', 2);
            if (partes.Length != 2 || !int.TryParse(partes[0], out var id))
            {
                return null;
            }
            var usuario = bancoDados.Usuarios.FirstOrDefault(u => u.ID == id);
            if (usuario == null || usuario.Email != partes[1])
            {
                return null;
            }
            if (usuario.EmailConfirmadoEm == null)
            {
                usuario.EmailConfirmadoEm = DateTime.UtcNow;
                bancoDados.SaveChanges();
            }
            return usuario;
        }

        /// <summary>E-mail de boas-vindas do cadastro, com o link de confirmação. Não salva (quem chama salva).</summary>
        public Task AdicionarBoasVindasAsync(Usuario usuario) =>
            fila.AdicionarAsync(TipoEmail.BoasVindas, usuario, "Bem-vindo(a) à CLOUUD — confirme seu e-mail", "BoasVindas",
                new EmailConfirmacaoViewModel(usuario.Name, GerarLink(usuario), links.Base, true));

        /// <summary>Só o link de confirmação (reenvio, ou e-mail trocado em "Minha conta"). Não salva.</summary>
        public Task AdicionarConfirmacaoAsync(Usuario usuario) =>
            fila.AdicionarAsync(TipoEmail.ConfirmarEmail, usuario, "Confirme seu e-mail na CLOUUD", "BoasVindas",
                new EmailConfirmacaoViewModel(usuario.Name, GerarLink(usuario), links.Base, false));

        /// <summary>Pode reenviar? Evita que alguém use o botão para lotar a caixa de outra pessoa.</summary>
        public bool PodeReenviar(Usuario usuario) =>
            !bancoDados.Emails.Any(e => e.UsuarioId == usuario.ID
                                        && (e.Tipo == TipoEmail.ConfirmarEmail || e.Tipo == TipoEmail.BoasVindas)
                                        && e.CriadoEm > DateTime.UtcNow - IntervaloReenvio);
    }

    public record EmailConfirmacaoViewModel(string Nome, string LinkConfirmacao, string UrlLoja, bool Cadastro);
}
