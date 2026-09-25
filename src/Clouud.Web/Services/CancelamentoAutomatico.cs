namespace Clouud.Web.Services
{
    /// <summary>
    /// Serviço em segundo plano: a cada minuto cancela os pedidos que passaram do prazo de pagamento,
    /// devolvendo as chaves reservadas para a loja.
    /// </summary>
    public class CancelamentoAutomatico : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory scopes;
        private readonly ILogger<CancelamentoAutomatico> logger;

        public CancelamentoAutomatico(IServiceScopeFactory scopes, ILogger<CancelamentoAutomatico> logger)
        {
            this.scopes = scopes;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var cancelados = scope.ServiceProvider.GetRequiredService<PedidoService>().CancelarVencidos();
                    if (cancelados > 0)
                    {
                        logger.LogInformation("{Quantidade} pedido(s) com prazo vencido cancelado(s)", cancelados);
                    }
                }
                catch (Exception erro) when (erro is not OperationCanceledException)
                {
                    // Ex.: banco fora do ar ou migrations não aplicadas. Tenta de novo no próximo minuto.
                    logger.LogWarning(erro, "Falha ao cancelar pedidos vencidos");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
