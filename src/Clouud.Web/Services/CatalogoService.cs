using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    /// <summary>Consultas da vitrine, usadas na página inicial pública e na área do cliente.</summary>
    public class CatalogoService
    {
        private readonly BancoDados bancoDados;

        public CatalogoService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        public VitrineViewModel MontarVitrine(string? busca, string? plataforma, string? categoria, bool somentePromocoes)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);

            // Só produtos ativos, de jogos ativos, em plataformas ativas
            var consulta = bancoDados.Produtos
                .Where(p => p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                consulta = consulta.Where(p => EF.Functions.ILike(p.Jogo.Titulo, $"%{busca.Trim()}%"));
            }
            if (!string.IsNullOrWhiteSpace(plataforma))
            {
                consulta = consulta.Where(p => p.Plataforma.Slug == plataforma);
            }
            if (!string.IsNullOrWhiteSpace(categoria))
            {
                consulta = consulta.Where(p => p.Jogo.Categorias.Any(c => c.Slug == categoria));
            }
            if (somentePromocoes)
            {
                consulta = consulta.Where(p => p.PrecoPromocional != null && p.PrecoPromocional < p.Preco
                                               && (p.PromocaoAte == null || p.PromocaoAte >= hoje));
            }

            // Esgotados vão para o fim da lista
            var produtos = Projetar(consulta
                .OrderByDescending(p => p.Chaves.Any(c => c.Status == StatusChave.Disponivel))
                .ThenByDescending(p => p.Jogo.Destaque)
                .ThenBy(p => p.Jogo.Titulo)
                .ThenBy(p => p.Plataforma.Nome), hoje)
                .ToList();

            return new VitrineViewModel
            {
                Busca = busca,
                Plataforma = plataforma,
                Categoria = categoria,
                SomentePromocoes = somentePromocoes,
                Produtos = produtos,
                Plataformas = bancoDados.Plataformas
                    .Where(p => p.Ativa && p.Produtos.Any(x => x.Ativo))
                    .OrderBy(p => p.Nome).ToList(),
                Categorias = bancoDados.Categorias
                    .Where(c => c.Jogos.Any(j => j.Ativo))
                    .OrderBy(c => c.Nome).ToList()
            };
        }

        /// <summary>Jogos da lista de desejos do usuário, com os produtos à venda de cada um.</summary>
        public List<DesejoViewModel> MontarListaDesejos(int usuarioId)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var desejos = bancoDados.ListaDesejos
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.AdicionadoEm)
                .Select(d => new DesejoViewModel
                {
                    JogoId = d.JogoId,
                    Titulo = d.Jogo.Titulo,
                    Capa = d.Jogo.Capa,
                    JogoAtivo = d.Jogo.Ativo,
                    AdicionadoEm = d.AdicionadoEm
                })
                .ToList();

            var jogoIds = desejos.Select(d => d.JogoId).ToList();
            var produtos = Projetar(bancoDados.Produtos
                .Where(p => jogoIds.Contains(p.JogoId) && p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa)
                .OrderBy(p => p.Plataforma.Nome).ThenBy(p => p.Edicao), hoje)
                .ToList();
            foreach (var desejo in desejos)
            {
                desejo.Produtos = produtos.Where(p => p.JogoId == desejo.JogoId).ToList();
            }
            return desejos;
        }

        private static IQueryable<ProdutoVitrineViewModel> Projetar(IQueryable<Produto> consulta, DateOnly hoje)
        {
            return consulta
                .Select(p => new ProdutoVitrineViewModel
                {
                    ProdutoId = p.Id,
                    JogoId = p.JogoId,
                    Titulo = p.Jogo.Titulo,
                    Capa = p.Jogo.Capa,
                    Plataforma = p.Plataforma.Nome,
                    Edicao = p.Edicao,
                    Categorias = p.Jogo.Categorias.OrderBy(c => c.Nome).Select(c => c.Nome).ToList(),
                    Desenvolvedora = p.Jogo.Desenvolvedora != null ? p.Jogo.Desenvolvedora.Nome : null,
                    Preco = p.Preco,
                    PrecoAtual = p.PrecoPromocional != null && p.PrecoPromocional < p.Preco
                                 && (p.PromocaoAte == null || p.PromocaoAte >= hoje)
                        ? p.PrecoPromocional.Value
                        : p.Preco,
                    Disponiveis = p.Chaves.Count(c => c.Status == StatusChave.Disponivel)
                });
        }
    }
}
