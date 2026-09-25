using Clouud.Web.Data;
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

            var produtos = consulta
                .OrderByDescending(p => p.Jogo.Destaque)
                .ThenBy(p => p.Jogo.Titulo)
                .ThenBy(p => p.Plataforma.Nome)
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
                        : p.Preco
                })
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
    }
}
