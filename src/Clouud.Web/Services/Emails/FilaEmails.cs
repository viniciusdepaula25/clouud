using Clouud.Web.Data;
using Clouud.Web.Models;

namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Põe e-mails na fila (tabela emails). Não chama SaveChanges: o e-mail é gravado junto com a
    /// operação que o gerou (ex.: o pagamento aprovado), então ou os dois ficam salvos ou nenhum.
    /// </summary>
    public class FilaEmails
    {
        private readonly BancoDados bancoDados;
        private readonly RenderizadorEmail renderizador;

        public FilaEmails(BancoDados bancoDados, RenderizadorEmail renderizador)
        {
            this.bancoDados = bancoDados;
            this.renderizador = renderizador;
        }

        /// <summary>Só monta o e-mail, sem pôr na fila.</summary>
        public Task<(string Html, string Texto)> RenderizarAsync<T>(string view, T modelo) => renderizador.RenderizarAsync(view, modelo);

        public async Task<Email> AdicionarAsync<T>(TipoEmail tipo, Usuario usuario, string assunto, string view, T modelo,
            string? para = null)
        {
            var (html, texto) = await renderizador.RenderizarAsync(view, modelo);
            var email = new Email
            {
                Tipo = tipo,
                UsuarioId = usuario.ID == 0 ? null : usuario.ID,
                Usuario = usuario.ID == 0 ? usuario : null,
                Para = para ?? usuario.Email,
                Assunto = assunto,
                Html = html,
                Texto = texto
            };
            bancoDados.Emails.Add(email);
            return email;
        }
    }
}
