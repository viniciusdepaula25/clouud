namespace Clouud.Web.Services.Emails
{
    /// <summary>Seção "Email" do appsettings.</summary>
    public class ConfiguracaoEmail
    {
        /// <summary>"Pasta": grava cada e-mail como arquivo .eml (desenvolvimento). "Smtp": envia de verdade.</summary>
        public string Modo { get; set; } = "Pasta";

        /// <summary>Pasta dos arquivos .eml no modo "Pasta" (relativa à pasta do projeto, se não for absoluta).</summary>
        public string Pasta { get; set; } = "emails-enviados";

        public string Remetente { get; set; } = "nao-responda@clouud.local";
        public string NomeRemetente { get; set; } = "CLOUUD";

        /// <summary>De quanto em quanto tempo a fila é conferida.</summary>
        public int IntervaloSegundos { get; set; } = 10;

        public ConfiguracaoSmtp Smtp { get; set; } = new();

        public bool UsaSmtp => string.Equals(Modo, "Smtp", StringComparison.OrdinalIgnoreCase);
    }

    public class ConfiguracaoSmtp
    {
        public string Host { get; set; } = "localhost";
        public int Porta { get; set; } = 587;
        public string? Usuario { get; set; }
        public string? Senha { get; set; }

        /// <summary>STARTTLS (porta 587 do Gmail, Outlook, Brevo...). O Mailpit do Docker não usa.</summary>
        public bool Ssl { get; set; } = true;
    }
}
