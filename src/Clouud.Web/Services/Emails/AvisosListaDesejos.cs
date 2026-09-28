using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Confere de tempos em tempos os jogos das listas de desejos e avisa o cliente por e-mail quando
    /// um jogo <b>volta ao estoque</b> ou <b>entra em promoção</b> (ou a promoção fica mais barata).
    /// Comparar com a situação guardada na conferência anterior pega todas as causas de uma vez:
    /// chaves importadas ou reativadas, pedido cancelado que devolveu chaves, promoção criada no admin...
    /// Só vai para quem confirmou o e-mail e não desligou os avisos.
    /// </summary>
    public class AvisosListaDesejos : BackgroundService
    {
        private readonly IServiceScopeFactory scopes;
        private readonly ILogger<AvisosListaDesejos> logger;
        private readonly TimeSpan intervalo;

        public AvisosListaDesejos(IServiceScopeFactory scopes, IConfiguration configuracao, ILogger<AvisosListaDesejos> logger)
        {
            this.scopes = scopes;
            this.logger = logger;
            intervalo = TimeSpan.FromSeconds(Math.Max(1, configuracao.GetValue("Loja:AvisosIntervaloSegundos", 60)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(intervalo);
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var avisos = await scope.ServiceProvider.GetRequiredService<ConferenciaListaDesejos>().ConferirAsync();
                    if (avisos > 0)
                    {
                        logger.LogInformation("{Quantidade} aviso(s) da lista de desejos na fila de e-mails", avisos);
                    }
                }
                catch (Exception erro) when (erro is not OperationCanceledException)
                {
                    logger.LogWarning(erro, "Falha ao conferir as listas de desejos");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }

    /// <summary>Uma conferência das listas de desejos (separada do serviço para rodar dentro de um escopo).</summary>
    public class ConferenciaListaDesejos
    {
        private readonly BancoDados bancoDados;
        private readonly FilaEmails fila;
        private readonly LinksLoja links;
        private readonly DescadastroAvisos descadastro;

        public ConferenciaListaDesejos(BancoDados bancoDados, FilaEmails fila, LinksLoja links, DescadastroAvisos descadastro)
        {
            this.bancoDados = bancoDados;
            this.fila = fila;
            this.links = links;
            this.descadastro = descadastro;
        }

        /// <summary>Devolve quantos avisos foram para a fila.</summary>
        public async Task<int> ConferirAsync()
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var desejos = bancoDados.ListaDesejos.Include(d => d.Usuario).Include(d => d.Jogo).ToList();
            if (desejos.Count == 0)
            {
                return 0;
            }

            // Produtos à venda dos jogos das listas, com chave disponível agora
            var jogoIds = desejos.Select(d => d.JogoId).Distinct().ToList();
            var produtos = bancoDados.Produtos
                .Where(p => jogoIds.Contains(p.JogoId) && p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa
                            && p.Chaves.Any(c => c.Status == StatusChave.Disponivel))
                .Select(p => new EmailAvisoProduto(
                    p.JogoId,
                    p.Plataforma.Nome,
                    p.Edicao,
                    p.Preco,
                    p.PrecoPromocional != null && p.PrecoPromocional < p.Preco && (p.PromocaoAte == null || p.PromocaoAte >= hoje)
                        ? p.PrecoPromocional
                        : null))
                .ToList();

            var avisos = 0;
            foreach (var desejo in desejos)
            {
                var doJogo = produtos.Where(p => p.JogoId == desejo.JogoId).OrderBy(p => p.PrecoAtual).ToList();
                var disponivel = doJogo.Count > 0;
                var promocao = doJogo.Where(p => p.PrecoPromocional.HasValue).Min(p => p.PrecoPromocional);

                if (desejo.AvisoConferidoEm != null)
                {
                    var voltou = disponivel && !desejo.AvisoDisponivel;
                    var promocaoNova = disponivel && promocao.HasValue
                                       && (desejo.AvisoPrecoPromocao == null || promocao < desejo.AvisoPrecoPromocao);
                    var usuario = desejo.Usuario;
                    if ((voltou || promocaoNova) && usuario.ReceberAvisos && usuario.EmailConfirmadoEm != null)
                    {
                        await AvisarAsync(desejo, doJogo, voltou, promocaoNova ? promocao : null);
                        avisos++;
                    }
                }
                else
                {
                    desejo.AvisoConferidoEm = DateTime.UtcNow; // primeira vez: só guarda a situação, sem e-mail
                }

                desejo.AvisoDisponivel = disponivel;
                desejo.AvisoPrecoPromocao = promocao;
            }
            await bancoDados.SaveChangesAsync();
            return avisos;
        }

        private async Task AvisarAsync(ListaDesejo desejo, List<EmailAvisoProduto> produtos, bool voltou, decimal? promocao)
        {
            var jogo = desejo.Jogo;
            var assunto = voltou && promocao.HasValue ? $"{jogo.Titulo} voltou ao estoque e está em promoção"
                : voltou ? $"{jogo.Titulo} voltou ao estoque"
                : $"{jogo.Titulo} entrou em promoção";
            await fila.AdicionarAsync(TipoEmail.ListaDesejos, desejo.Usuario, assunto, "AvisoListaDesejos",
                new EmailAvisoViewModel(desejo.Usuario.Name, jogo.Titulo,
                    string.IsNullOrEmpty(jogo.Capa) ? null : links.Absoluto("/uploads/" + jogo.Capa),
                    links.Absoluto($"/jogo/{jogo.Slug}"), voltou, promocao, produtos,
                    descadastro.GerarLink(desejo.Usuario), links.Absoluto("/Cliente/ListaDesejos")));
        }
    }

    /// <summary>Link "parar de receber" dos avisos, que funciona sem login (código assinado, sem validade).</summary>
    public class DescadastroAvisos
    {
        private readonly IDataProtector protetor;
        private readonly LinksLoja links;

        public DescadastroAvisos(IDataProtectionProvider protecao, LinksLoja links)
        {
            protetor = protecao.CreateProtector("Clouud.PararAvisos");
            this.links = links;
        }

        public string GerarLink(Usuario usuario) =>
            links.Absoluto($"/conta/parar-avisos?codigo={Uri.EscapeDataString(protetor.Protect(usuario.ID.ToString()))}");

        public int? LerUsuario(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }
            try
            {
                return int.TryParse(protetor.Unprotect(codigo), out var id) ? id : null;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
        }
    }

    public record EmailAvisoProduto(int JogoId, string Plataforma, string Edicao, decimal Preco, decimal? PrecoPromocional)
    {
        public decimal PrecoAtual => PrecoPromocional ?? Preco;
        public int PercentualDesconto => PrecoPromocional.HasValue && Preco > 0
            ? (int)Math.Round((1 - PrecoPromocional.Value / Preco) * 100) : 0;
    }

    public record EmailAvisoViewModel(string Nome, string Jogo, string? Capa, string LinkJogo, bool VoltouAoEstoque,
        decimal? PrecoPromocional, List<EmailAvisoProduto> Produtos, string LinkDescadastro, string LinkLista);
}
