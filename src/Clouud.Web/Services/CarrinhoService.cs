using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>Carrinho do cliente, guardado no banco (continua lá se ele sair e voltar).</summary>
    public class CarrinhoService
    {
        private readonly BancoDados bancoDados;

        public CarrinhoService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        public CarrinhoViewModel Montar(int usuarioId)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var itens = bancoDados.CarrinhoItens
                .Where(i => i.UsuarioId == usuarioId)
                .OrderBy(i => i.AdicionadoEm).ThenBy(i => i.Id)
                .Select(i => new
                {
                    i.Quantidade,
                    Produto = i.Produto,
                    i.Produto.Jogo.Titulo,
                    i.Produto.Jogo.Capa,
                    Plataforma = i.Produto.Plataforma.Nome,
                    AVenda = i.Produto.Ativo && i.Produto.Jogo.Ativo && i.Produto.Plataforma.Ativa,
                    Disponiveis = i.Produto.Chaves.Count(c => c.Status == StatusChave.Disponivel)
                })
                .ToList();

            return new CarrinhoViewModel
            {
                Itens = itens.Select(i => new CarrinhoLinhaViewModel
                {
                    ProdutoId = i.Produto.Id,
                    JogoId = i.Produto.JogoId,
                    Titulo = i.Titulo,
                    Capa = i.Capa,
                    Plataforma = i.Plataforma,
                    Edicao = i.Produto.Edicao,
                    Quantidade = i.Quantidade,
                    Preco = i.Produto.Preco,
                    PrecoAtual = i.Produto.PrecoAtual(hoje),
                    AVenda = i.AVenda,
                    Disponiveis = i.Disponiveis
                }).ToList()
            };
        }

        /// <summary>Total de unidades no carrinho (para o contador da barra de navegação).</summary>
        public int ContarUnidades(int usuarioId)
        {
            return bancoDados.CarrinhoItens.Where(i => i.UsuarioId == usuarioId).Sum(i => (int?)i.Quantidade) ?? 0;
        }

        /// <summary>Adiciona uma unidade do produto. Devolve a mensagem de erro, ou null se deu certo.</summary>
        public string? Adicionar(int usuarioId, int produtoId)
        {
            var produto = bancoDados.Produtos
                .Where(p => p.Id == produtoId && p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa)
                .Select(p => new { p.Id, Disponiveis = p.Chaves.Count(c => c.Status == StatusChave.Disponivel) })
                .FirstOrDefault();
            if (produto == null)
            {
                return "Este produto não está à venda.";
            }
            if (produto.Disponiveis == 0)
            {
                return "Este produto está esgotado.";
            }

            var item = bancoDados.CarrinhoItens.FirstOrDefault(i => i.UsuarioId == usuarioId && i.ProdutoId == produtoId);
            if (item == null)
            {
                bancoDados.CarrinhoItens.Add(new CarrinhoItem { UsuarioId = usuarioId, ProdutoId = produtoId, Quantidade = 1 });
            }
            else
            {
                var limite = Math.Min(CarrinhoItem.QuantidadeMaxima, produto.Disponiveis);
                if (item.Quantidade >= limite)
                {
                    return item.Quantidade >= CarrinhoItem.QuantidadeMaxima
                        ? $"O limite é de {CarrinhoItem.QuantidadeMaxima} unidades por produto."
                        : $"Só restam {produto.Disponiveis} unidades deste produto.";
                }
                item.Quantidade++;
            }

            try
            {
                bancoDados.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Dois cliques ao mesmo tempo: o outro já adicionou o produto
                bancoDados.ChangeTracker.Clear();
            }
            return null;
        }

        /// <summary>Muda a quantidade (limitada ao máximo por produto). Devolve um aviso, ou null.</summary>
        public string? Atualizar(int usuarioId, int produtoId, int quantidade)
        {
            var item = bancoDados.CarrinhoItens.FirstOrDefault(i => i.UsuarioId == usuarioId && i.ProdutoId == produtoId);
            if (item == null)
            {
                return null;
            }

            item.Quantidade = Math.Clamp(quantidade, 1, CarrinhoItem.QuantidadeMaxima);
            bancoDados.SaveChanges();
            return quantidade > CarrinhoItem.QuantidadeMaxima
                ? $"O limite é de {CarrinhoItem.QuantidadeMaxima} unidades por produto."
                : null;
        }

        public void Remover(int usuarioId, int produtoId)
        {
            bancoDados.CarrinhoItens
                .Where(i => i.UsuarioId == usuarioId && i.ProdutoId == produtoId)
                .ExecuteDelete();
        }
    }
}
