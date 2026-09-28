namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Endereço público da loja, para os links dos e-mails. Vem da configuração "Loja:UrlPublica"
    /// e não do cabeçalho Host da requisição, que um atacante poderia trocar para receber links de senha.
    /// </summary>
    public class LinksLoja
    {
        public LinksLoja(IConfiguration configuracao)
        {
            Base = (configuracao["Loja:UrlPublica"] ?? "http://localhost:5093").TrimEnd('/');
        }

        public string Base { get; }

        public string Absoluto(string caminho) => Base + "/" + caminho.TrimStart('/');
    }
}
