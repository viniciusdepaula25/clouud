using System.Net;
using System.Net.Mail;
using System.Text;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Serviço em segundo plano que esvazia a fila de e-mails: manda os pendentes, marca como enviados
    /// e apaga o conteúdo. Se o servidor de e-mail falhar, tenta de novo nas próximas voltas
    /// (até <see cref="Email.MaximoTentativas"/> vezes) e guarda o erro para aparecer no admin.
    /// </summary>
    public class EnvioEmails : BackgroundService
    {
        private readonly IServiceScopeFactory scopes;
        private readonly ConfiguracaoEmail configuracao;
        private readonly IWebHostEnvironment ambiente;
        private readonly ILogger<EnvioEmails> logger;

        public EnvioEmails(IServiceScopeFactory scopes, IOptions<ConfiguracaoEmail> configuracao,
            IWebHostEnvironment ambiente, ILogger<EnvioEmails> logger)
        {
            this.scopes = scopes;
            this.configuracao = configuracao.Value;
            this.ambiente = ambiente;
            this.logger = logger;
        }

        public string PastaDosArquivos => Path.IsPathRooted(configuracao.Pasta)
            ? configuracao.Pasta
            : Path.Combine(ambiente.ContentRootPath, configuracao.Pasta);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(configuracao.UsaSmtp
                    ? "E-mails: envio por SMTP ({Host}:{Porta})"
                    : "E-mails: gravados como arquivos .eml em {Pasta}",
                configuracao.UsaSmtp ? configuracao.Smtp.Host : PastaDosArquivos, configuracao.Smtp.Porta);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, configuracao.IntervaloSegundos)));
            do
            {
                try
                {
                    await EnviarPendentesAsync(stoppingToken);
                }
                catch (Exception erro) when (erro is not OperationCanceledException)
                {
                    // Ex.: banco fora do ar ou migrations não aplicadas. Tenta de novo na próxima volta.
                    logger.LogWarning(erro, "Falha ao processar a fila de e-mails");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        public async Task<int> EnviarPendentesAsync(CancellationToken cancelar = default)
        {
            using var scope = scopes.CreateScope();
            var bancoDados = scope.ServiceProvider.GetRequiredService<BancoDados>();
            var pendentes = await bancoDados.Emails
                .Where(e => e.EnviadoEm == null && e.Tentativas < Email.MaximoTentativas)
                .OrderBy(e => e.Id)
                .Take(50)
                .ToListAsync(cancelar);

            var enviados = 0;
            foreach (var email in pendentes)
            {
                try
                {
                    await EnviarAsync(email, cancelar);
                    email.EnviadoEm = DateTime.UtcNow;
                    email.UltimoErro = null;
                    email.Html = null;   // não guarda links de senha nem chaves depois de enviar
                    email.Texto = null;
                    enviados++;
                }
                catch (Exception erro) when (erro is not OperationCanceledException)
                {
                    email.UltimoErro = erro.Message.Length > 500 ? erro.Message[..500] : erro.Message;
                    logger.LogWarning("Falha ao enviar o e-mail {Id} para {Para}: {Erro}", email.Id, email.Para, erro.Message);
                }
                email.Tentativas++;
                await bancoDados.SaveChangesAsync(cancelar);
            }
            return enviados;
        }

        private async Task EnviarAsync(Email email, CancellationToken cancelar)
        {
            using var mensagem = new MailMessage
            {
                From = new MailAddress(configuracao.Remetente, configuracao.NomeRemetente, Encoding.UTF8),
                Subject = email.Assunto,
                SubjectEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Body = email.Texto ?? "",
                IsBodyHtml = false
            };
            mensagem.To.Add(new MailAddress(email.Para));
            if (!string.IsNullOrEmpty(email.Html))
            {
                mensagem.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.Html, Encoding.UTF8, "text/html"));
            }

            using var cliente = new SmtpClient();
            if (configuracao.UsaSmtp)
            {
                cliente.Host = configuracao.Smtp.Host;
                cliente.Port = configuracao.Smtp.Porta;
                cliente.EnableSsl = configuracao.Smtp.Ssl;
                if (!string.IsNullOrEmpty(configuracao.Smtp.Usuario))
                {
                    cliente.Credentials = new NetworkCredential(configuracao.Smtp.Usuario, configuracao.Smtp.Senha);
                }
            }
            else
            {
                Directory.CreateDirectory(PastaDosArquivos);
                cliente.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                cliente.PickupDirectoryLocation = PastaDosArquivos;
            }
            await cliente.SendMailAsync(mensagem, cancelar);
        }
    }
}
