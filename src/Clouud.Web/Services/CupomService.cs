using Clouud.Web.Data;
using Clouud.Web.Models;

namespace Clouud.Web.Services
{
    /// <summary>Resultado da validação de um cupom para um carrinho.</summary>
    public record ResultadoCupom(Cupom? Cupom, decimal Desconto, string? Erro)
    {
        public bool Valido => Cupom != null && Erro == null;
    }

    /// <summary>Regras dos cupons de desconto.</summary>
    public class CupomService
    {
        /// <summary>Pedidos que "gastam" um uso do cupom. Cancelados e reembolsados devolvem o uso.</summary>
        public static readonly StatusPedido[] StatusQueUsam = [StatusPedido.AguardandoPagamento, StatusPedido.Pago];

        private readonly BancoDados bancoDados;

        public CupomService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        public static string Normalizar(string? codigo) => (codigo ?? string.Empty).Trim().ToUpperInvariant();

        public int ContarUsos(int cupomId) =>
            bancoDados.Pedidos.Count(p => p.CupomId == cupomId && StatusQueUsam.Contains(p.Status));

        /// <summary>Confere se o cupom pode ser usado por este cliente neste subtotal e calcula o desconto.</summary>
        public ResultadoCupom Validar(string? codigo, int usuarioId, decimal subtotal)
        {
            var normalizado = Normalizar(codigo);
            if (normalizado.Length == 0)
            {
                return new ResultadoCupom(null, 0, "Digite o código do cupom.");
            }
            return Validar(bancoDados.Cupons.FirstOrDefault(c => c.Codigo == normalizado), normalizado, usuarioId, subtotal);
        }

        /// <summary>Mesma validação, para um cupom já lido (e travado) pelo pedido.</summary>
        public ResultadoCupom Validar(Cupom? cupom, string codigo, int usuarioId, decimal subtotal)
        {
            if (cupom == null || !cupom.Ativo)
            {
                return new ResultadoCupom(null, 0, $"O cupom {codigo} não existe ou não está mais ativo.");
            }

            var hoje = DateOnly.FromDateTime(DateTime.Now);
            if (cupom.ValidoDe.HasValue && hoje < cupom.ValidoDe)
            {
                return new ResultadoCupom(cupom, 0, $"O cupom {cupom.Codigo} só vale a partir de {cupom.ValidoDe:dd/MM/yyyy}.");
            }
            if (cupom.ValidoAte.HasValue && hoje > cupom.ValidoAte)
            {
                return new ResultadoCupom(cupom, 0, $"O cupom {cupom.Codigo} venceu em {cupom.ValidoAte:dd/MM/yyyy}.");
            }
            if (cupom.PedidoMinimo.HasValue && subtotal < cupom.PedidoMinimo)
            {
                return new ResultadoCupom(cupom, 0,
                    $"O cupom {cupom.Codigo} vale para compras a partir de {cupom.PedidoMinimo.Value.ToString("C")}.");
            }
            if (cupom.LimiteUsos.HasValue && ContarUsos(cupom.Id) >= cupom.LimiteUsos)
            {
                return new ResultadoCupom(cupom, 0, $"O cupom {cupom.Codigo} já foi usado o máximo de vezes.");
            }
            if (cupom.LimitePorCliente.HasValue)
            {
                var usosDoCliente = bancoDados.Pedidos.Count(p => p.CupomId == cupom.Id && p.UsuarioId == usuarioId
                                                                  && StatusQueUsam.Contains(p.Status));
                if (usosDoCliente >= cupom.LimitePorCliente)
                {
                    return new ResultadoCupom(cupom, 0, cupom.LimitePorCliente == 1
                        ? $"Você já usou o cupom {cupom.Codigo}."
                        : $"Você já usou o cupom {cupom.Codigo} {cupom.LimitePorCliente} vezes.");
                }
            }

            return new ResultadoCupom(cupom, cupom.CalcularDesconto(subtotal), null);
        }
    }
}
