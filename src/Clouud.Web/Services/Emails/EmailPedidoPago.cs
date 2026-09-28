using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services.Emails
{
    /// <summary>E-mail de pedido pago: resumo da compra, as chaves e como ativar em cada plataforma.</summary>
    public class EmailPedidoPago
    {
        private readonly BancoDados bancoDados;
        private readonly FilaEmails fila;
        private readonly LinksLoja links;

        public EmailPedidoPago(BancoDados bancoDados, FilaEmails fila, LinksLoja links)
        {
            this.bancoDados = bancoDados;
            this.fila = fila;
            this.links = links;
        }

        /// <summary>Põe o e-mail na fila (quem chama salva, dentro da mesma transação do pagamento).</summary>
        public async Task AdicionarAsync(int pedidoId)
        {
            var pedido = bancoDados.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Cupom)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Jogo)
                .Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.Plataforma)
                .AsSplitQuery()
                .First(p => p.Id == pedidoId);
            var itemIds = pedido.Itens.Select(i => i.Id).ToList();
            var chaves = bancoDados.Chaves
                .Where(c => c.PedidoItemId != null && itemIds.Contains(c.PedidoItemId.Value) && c.Status == StatusChave.Vendida)
                .OrderBy(c => c.Id)
                .Select(c => new { c.PedidoItemId, c.Codigo })
                .ToList();

            var modelo = new EmailPedidoPagoViewModel(
                pedido.Usuario.Name,
                pedido.Id,
                pedido.Itens.OrderBy(i => i.Id).Select(i => new EmailPedidoItem(
                    i.Produto.Jogo.Titulo,
                    i.Produto.Plataforma.Nome,
                    i.Produto.Edicao,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal,
                    chaves.Where(c => c.PedidoItemId == i.Id).Select(c => c.Codigo).ToList(),
                    i.Produto.Plataforma.InstrucoesAtivacao)).ToList(),
                pedido.Subtotal,
                pedido.Desconto,
                pedido.Cupom?.Codigo,
                pedido.Total,
                links.Absoluto($"/Cliente/Pedidos/Detalhes/{pedido.Id}"),
                links.Absoluto("/Cliente/MinhasChaves"));

            await fila.AdicionarAsync(TipoEmail.PedidoPago, pedido.Usuario,
                $"Pedido #{pedido.Id} aprovado — suas chaves chegaram", "PedidoPago", modelo);
        }
    }

    public record EmailPedidoPagoViewModel(string Nome, int PedidoId, List<EmailPedidoItem> Itens, decimal Subtotal,
        decimal Desconto, string? Cupom, decimal Total, string LinkPedido, string LinkMinhasChaves);

    public record EmailPedidoItem(string Jogo, string Plataforma, string Edicao, int Quantidade, decimal PrecoUnitario,
        decimal Subtotal, List<string> Chaves, string? ComoAtivar);
}
