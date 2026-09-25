using System.Data;
using System.Security.Cryptography;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Ciclo de vida do pedido e das chaves:
    /// criar (chaves Reservadas) → pagar (Vendidas) | cancelar ou vencer o prazo (voltam a Disponível);
    /// pedido pago e reembolsado → chaves Inativas, porque o cliente já viu os códigos.
    /// </summary>
    public class PedidoService
    {
        private readonly BancoDados bancoDados;
        private readonly ILogger<PedidoService> logger;

        public PedidoService(BancoDados bancoDados, IConfiguration configuracao, ILogger<PedidoService> logger)
        {
            this.bancoDados = bancoDados;
            this.logger = logger;
            PrazoPagamento = TimeSpan.FromMinutes(configuracao.GetValue("Loja:MinutosParaPagar", 30));
        }

        /// <summary>Tempo que as chaves ficam reservadas esperando o pagamento.</summary>
        public TimeSpan PrazoPagamento { get; }

        /// <summary>Transforma o carrinho em pedido e reserva as chaves. Devolve o pedido ou a mensagem de erro.</summary>
        public (Pedido? Pedido, string? Erro) CriarDoCarrinho(int usuarioId)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            using var transacao = bancoDados.Database.BeginTransaction(IsolationLevel.ReadCommitted);

            var itens = bancoDados.CarrinhoItens
                .Include(i => i.Produto).ThenInclude(p => p.Jogo)
                .Include(i => i.Produto).ThenInclude(p => p.Plataforma)
                .Where(i => i.UsuarioId == usuarioId)
                .OrderBy(i => i.Id)
                .ToList();
            if (itens.Count == 0)
            {
                return (null, "Seu carrinho está vazio.");
            }

            var foraDeVenda = itens.FirstOrDefault(i => !(i.Produto.Ativo && i.Produto.Jogo.Ativo && i.Produto.Plataforma.Ativa));
            if (foraDeVenda != null)
            {
                return (null, $"\"{foraDeVenda.Produto.Jogo.Titulo}\" não está mais à venda. Remova do carrinho para continuar.");
            }

            var agora = DateTime.UtcNow;
            var pedido = new Pedido
            {
                UsuarioId = usuarioId,
                Status = StatusPedido.AguardandoPagamento,
                CriadoEm = agora,
                PagarAte = agora + PrazoPagamento
            };
            foreach (var item in itens)
            {
                var preco = item.Produto.PrecoAtual(hoje);   // preço congelado no momento da compra
                pedido.Itens.Add(new PedidoItem
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = preco,
                    Subtotal = preco * item.Quantidade
                });
            }
            pedido.Total = pedido.Itens.Sum(i => i.Subtotal);
            bancoDados.Pedidos.Add(pedido);
            bancoDados.SaveChanges();

            // Reserva as chaves. FOR UPDATE SKIP LOCKED: duas compras ao mesmo tempo nunca pegam a mesma chave
            foreach (var item in pedido.Itens)
            {
                var reservadas = bancoDados.Database.ExecuteSqlInterpolated($"""
                    UPDATE chaves SET status = 'Reservada', pedido_item_id = {item.Id}
                    WHERE id IN (SELECT id FROM chaves
                                 WHERE produto_id = {item.ProdutoId} AND status = 'Disponivel'
                                 ORDER BY id LIMIT {item.Quantidade}
                                 FOR UPDATE SKIP LOCKED)
                    """);
                if (reservadas < item.Quantidade)
                {
                    transacao.Rollback();
                    bancoDados.ChangeTracker.Clear();
                    var titulo = itens.First(i => i.ProdutoId == item.ProdutoId).Produto.Jogo.Titulo;
                    return (null, reservadas == 0 && item.Quantidade == 1
                        ? $"\"{titulo}\" acabou de esgotar. Remova do carrinho para continuar."
                        : $"Não há chaves suficientes de \"{titulo}\" agora. Diminua a quantidade e tente de novo.");
                }
            }

            bancoDados.CarrinhoItens.RemoveRange(itens);
            bancoDados.SaveChanges();
            transacao.Commit();

            logger.LogInformation("Pedido {PedidoId} criado pelo usuário {UsuarioId}: {Total}", pedido.Id, usuarioId, pedido.Total);
            return (pedido, null);
        }

        /// <summary>
        /// Pagamento simulado: registra a tentativa e, se aprovada, entrega as chaves.
        /// Devolve a mensagem de erro, ou null se o pagamento foi registrado.
        /// </summary>
        public string? Pagar(int pedidoId, int usuarioId, MetodoPagamento metodo, bool aprovar)
        {
            using var transacao = bancoDados.Database.BeginTransaction(IsolationLevel.ReadCommitted);
            var pedido = TravarPedido(pedidoId);
            if (pedido == null || pedido.UsuarioId != usuarioId)
            {
                return "Pedido não encontrado.";
            }
            if (pedido.Status != StatusPedido.AguardandoPagamento)
            {
                return "Este pedido não está mais aguardando pagamento.";
            }

            var agora = DateTime.UtcNow;
            if (pedido.PagarAte < agora)
            {
                MudarParaCancelado(pedido);
                transacao.Commit();
                return "O prazo para pagar terminou. O pedido foi cancelado e as chaves voltaram para a loja.";
            }

            bancoDados.Pagamentos.Add(new Pagamento
            {
                PedidoId = pedido.Id,
                Metodo = metodo,
                Valor = pedido.Total,
                Status = aprovar ? StatusPagamento.Aprovado : StatusPagamento.Recusado,
                CodigoTransacao = "SIM-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
                CriadoEm = agora,
                ConfirmadoEm = aprovar ? agora : null
            });

            if (aprovar)
            {
                pedido.Status = StatusPedido.Pago;
                pedido.PagoEm = agora;
                bancoDados.Chaves
                    .Where(c => c.PedidoItem!.PedidoId == pedido.Id && c.Status == StatusChave.Reservada)
                    .ExecuteUpdate(s => s
                        .SetProperty(c => c.Status, StatusChave.Vendida)
                        .SetProperty(c => c.VendidaEm, agora));
            }

            bancoDados.SaveChanges();
            transacao.Commit();
            logger.LogInformation("Pagamento simulado do pedido {PedidoId}: {Resultado}", pedido.Id, aprovar ? "aprovado" : "recusado");
            return null;
        }

        /// <summary>
        /// Cancela um pedido que ainda não foi pago e devolve as chaves ao estoque.
        /// Com <paramref name="usuarioId"/> só cancela pedidos daquele usuário; sem ele (admin), qualquer um.
        /// </summary>
        public string? Cancelar(int pedidoId, int? usuarioId)
        {
            using var transacao = bancoDados.Database.BeginTransaction(IsolationLevel.ReadCommitted);
            var pedido = TravarPedido(pedidoId);
            if (pedido == null || (usuarioId.HasValue && pedido.UsuarioId != usuarioId))
            {
                return "Pedido não encontrado.";
            }
            if (pedido.Status != StatusPedido.AguardandoPagamento)
            {
                return "Só pedidos aguardando pagamento podem ser cancelados.";
            }

            MudarParaCancelado(pedido);
            transacao.Commit();
            return null;
        }

        /// <summary>Reembolso (admin): o pedido pago volta o dinheiro e as chaves ficam inativas.</summary>
        public string? Reembolsar(int pedidoId)
        {
            using var transacao = bancoDados.Database.BeginTransaction(IsolationLevel.ReadCommitted);
            var pedido = TravarPedido(pedidoId);
            if (pedido == null)
            {
                return "Pedido não encontrado.";
            }
            if (pedido.Status != StatusPedido.Pago)
            {
                return "Só pedidos pagos podem ser reembolsados.";
            }

            pedido.Status = StatusPedido.Reembolsado;
            bancoDados.Chaves
                .Where(c => c.PedidoItem!.PedidoId == pedido.Id && c.Status == StatusChave.Vendida)
                .ExecuteUpdate(s => s.SetProperty(c => c.Status, StatusChave.Inativa));
            bancoDados.SaveChanges();
            transacao.Commit();
            logger.LogInformation("Pedido {PedidoId} reembolsado", pedido.Id);
            return null;
        }

        /// <summary>Cancela os pedidos com prazo de pagamento vencido. Devolve quantos foram cancelados.</summary>
        public int CancelarVencidos()
        {
            var agora = DateTime.UtcNow;
            var vencidos = bancoDados.Pedidos
                .Where(p => p.Status == StatusPedido.AguardandoPagamento && p.PagarAte < agora)
                .Select(p => p.Id)
                .ToList();

            var cancelados = 0;
            foreach (var id in vencidos)
            {
                if (Cancelar(id, null) == null)
                {
                    cancelados++;
                }
                bancoDados.ChangeTracker.Clear();
            }
            return cancelados;
        }

        /// <summary>Lê o pedido travando a linha até o fim da transação (pagar e cancelar não se cruzam).</summary>
        private Pedido? TravarPedido(int pedidoId)
        {
            return bancoDados.Pedidos
                .FromSqlInterpolated($"SELECT * FROM pedidos WHERE id = {pedidoId} FOR UPDATE")
                .AsEnumerable()
                .FirstOrDefault();
        }

        private void MudarParaCancelado(Pedido pedido)
        {
            pedido.Status = StatusPedido.Cancelado;
            bancoDados.Chaves
                .Where(c => c.PedidoItem!.PedidoId == pedido.Id && c.Status == StatusChave.Reservada)
                .ExecuteUpdate(s => s
                    .SetProperty(c => c.Status, StatusChave.Disponivel)
                    .SetProperty(c => c.PedidoItemId, (int?)null));
            bancoDados.SaveChanges();
            logger.LogInformation("Pedido {PedidoId} cancelado; chaves reservadas voltaram ao estoque", pedido.Id);
        }
    }
}
