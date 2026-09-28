using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>Números da tela inicial do admin.</summary>
    public class PainelService
    {
        private readonly BancoDados bancoDados;

        public PainelService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        public PainelViewModel Montar(int dias)
        {
            if (!PainelViewModel.PeriodosPermitidos.Contains(dias))
            {
                dias = 30;
            }

            // O período conta dias inteiros no horário local: "7 dias" = hoje e os 6 dias anteriores
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var primeiroDia = hoje.AddDays(-(dias - 1));
            var inicio = ParaUtc(primeiroDia);
            var inicioAnterior = ParaUtc(primeiroDia.AddDays(-dias));

            var pagos = bancoDados.Pedidos
                .Where(p => p.Status == StatusPedido.Pago && p.PagoEm >= inicioAnterior)
                .Select(p => new { p.Id, PagoEm = p.PagoEm!.Value, p.Total, Unidades = p.Itens.Sum(i => i.Quantidade) })
                .ToList();
            var doPeriodo = pagos.Where(p => p.PagoEm >= inicio).ToList();
            var doAnterior = pagos.Where(p => p.PagoEm < inicio).ToList();

            var porDia = doPeriodo
                .GroupBy(p => DateOnly.FromDateTime(p.PagoEm.ToLocalTime()))
                .ToDictionary(g => g.Key, g => (Pedidos: g.Count(), Valor: g.Sum(p => p.Total)));

            var idsDoPeriodo = doPeriodo.Select(p => p.Id).ToList();
            var maisVendidos = bancoDados.PedidoItens
                .Where(i => idsDoPeriodo.Contains(i.PedidoId))
                .GroupBy(i => new { i.ProdutoId, Jogo = i.Produto.Jogo.Titulo, Plataforma = i.Produto.Plataforma.Nome, i.Produto.Edicao })
                .Select(g => new MaisVendidoViewModel
                {
                    ProdutoId = g.Key.ProdutoId,
                    Jogo = g.Key.Jogo,
                    Plataforma = g.Key.Plataforma,
                    Edicao = g.Key.Edicao,
                    Unidades = g.Sum(i => i.Quantidade),
                    Receita = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(m => m.Unidades).ThenByDescending(m => m.Receita)
                .Take(5)
                .ToList();

            // Produtos à venda com estoque baixo, os mais procurados (lista de desejos) primeiro
            var aVenda = bancoDados.Produtos.Where(p => p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa);
            var repor = aVenda
                .Select(p => new EstoqueProdutoViewModel
                {
                    ProdutoId = p.Id,
                    Jogo = p.Jogo.Titulo,
                    Plataforma = p.Plataforma.Nome,
                    Edicao = p.Edicao,
                    Ativo = true,
                    Disponiveis = p.Chaves.Count(c => c.Status == StatusChave.Disponivel),
                    Desejos = bancoDados.ListaDesejos.Count(d => d.JogoId == p.JogoId)
                })
                .Where(p => p.Disponiveis < EstoqueProdutoViewModel.EstoqueBaixo)
                .OrderByDescending(p => p.Desejos).ThenBy(p => p.Disponiveis).ThenBy(p => p.Jogo)
                .ToList();

            var aguardando = bancoDados.Pedidos.Where(p => p.Status == StatusPedido.AguardandoPagamento);

            return new PainelViewModel
            {
                Dias = dias,
                Faturamento = doPeriodo.Sum(p => p.Total),
                FaturamentoAnterior = doAnterior.Sum(p => p.Total),
                PedidosPagos = doPeriodo.Count,
                PedidosPagosAnterior = doAnterior.Count,
                ChavesVendidas = doPeriodo.Sum(p => p.Unidades),
                VendasPorDia = Enumerable.Range(0, dias)
                    .Select(i => primeiroDia.AddDays(i))
                    .Select(dia => porDia.TryGetValue(dia, out var v)
                        ? new VendaDiaViewModel { Dia = dia, Pedidos = v.Pedidos, Valor = v.Valor }
                        : new VendaDiaViewModel { Dia = dia })
                    .ToList(),
                MaisVendidos = maisVendidos,
                AguardandoPagamento = aguardando.Count(),
                ValorAguardando = aguardando.Sum(p => (decimal?)p.Total) ?? 0,
                ProdutosEsgotados = repor.Count(p => p.Disponiveis == 0),
                ChavesDisponiveis = bancoDados.Chaves.Count(c => c.Status == StatusChave.Disponivel && c.Produto.Ativo),
                Clientes = bancoDados.Usuarios.Count(u => u.Perfil == PerfilUsuario.Cliente),
                Repor = repor.Take(8).ToList(),
                UltimosPedidos = bancoDados.Pedidos
                    .Include(p => p.Usuario)
                    .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
                    .Take(6)
                    .ToList()
            };
        }

        private static DateTime ParaUtc(DateOnly dia) =>
            dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
    }
}
