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

        /// <summary>Produtos à venda: ativos, de jogos ativos, em plataformas ativas.</summary>
        private IQueryable<Produto> AVenda() =>
            bancoDados.Produtos.Where(p => p.Ativo && p.Jogo.Ativo && p.Plataforma.Ativa);

        public VitrineViewModel MontarVitrine(FiltroVitrine filtro)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var consulta = AVenda();

            if (!string.IsNullOrWhiteSpace(filtro.Busca))
            {
                consulta = consulta.Where(p => EF.Functions.ILike(p.Jogo.Titulo, $"%{filtro.Busca.Trim()}%"));
            }
            if (!string.IsNullOrWhiteSpace(filtro.Plataforma))
            {
                consulta = consulta.Where(p => p.Plataforma.Slug == filtro.Plataforma);
            }
            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                consulta = consulta.Where(p => p.Jogo.Categorias.Any(c => c.Slug == filtro.Categoria));
            }
            if (filtro.Promocoes)
            {
                consulta = consulta.Where(p => p.PrecoPromocional != null && p.PrecoPromocional < p.Preco
                                               && (p.PromocaoAte == null || p.PromocaoAte >= hoje));
            }

            // Faixa de preço sobre o preço de agora (com a promoção); "de 100 até 50" é lido como "de 50 até 100"
            if (filtro.PrecoMin.HasValue && filtro.PrecoMax.HasValue && filtro.PrecoMin > filtro.PrecoMax)
            {
                (filtro.PrecoMin, filtro.PrecoMax) = (filtro.PrecoMax, filtro.PrecoMin);
            }
            var produtos = Projetar(consulta, hoje);
            if (filtro.PrecoMin.HasValue)
            {
                var minimo = filtro.PrecoMin.Value;
                produtos = produtos.Where(p => p.PrecoAtual >= minimo);
            }
            if (filtro.PrecoMax.HasValue)
            {
                var maximo = filtro.PrecoMax.Value;
                produtos = produtos.Where(p => p.PrecoAtual <= maximo);
            }

            // Esgotados sempre no fim; dentro de cada grupo, a ordem escolhida
            var comEstoquePrimeiro = produtos.OrderByDescending(p => p.Disponiveis > 0);
            var ordenados = filtro.OrdemEfetiva switch
            {
                "menor-preco" => comEstoquePrimeiro.ThenBy(p => p.PrecoAtual).ThenBy(p => p.Titulo),
                "maior-preco" => comEstoquePrimeiro.ThenByDescending(p => p.PrecoAtual).ThenBy(p => p.Titulo),
                "lancamentos" => comEstoquePrimeiro.ThenByDescending(p => p.DataLancamento.HasValue)
                                                   .ThenByDescending(p => p.DataLancamento).ThenBy(p => p.Titulo),
                "mais-vendidos" => comEstoquePrimeiro.ThenByDescending(p => p.Vendidos).ThenBy(p => p.Titulo),
                _ => comEstoquePrimeiro.ThenByDescending(p => p.Destaque).ThenBy(p => p.Titulo),
            };

            var total = ordenados.Count();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)FiltroVitrine.TamanhoPagina));
            var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
            filtro.Pagina = pagina;

            return new VitrineViewModel
            {
                Filtro = filtro,
                TotalProdutos = total,
                Pagina = pagina,
                Produtos = ordenados
                    .ThenBy(p => p.Plataforma).ThenBy(p => p.ProdutoId)
                    .Skip((pagina - 1) * FiltroVitrine.TamanhoPagina)
                    .Take(FiltroVitrine.TamanhoPagina)
                    .ToList(),
                Plataformas = bancoDados.Plataformas
                    .Where(p => p.Ativa && p.Produtos.Any(x => x.Ativo))
                    .OrderBy(p => p.Nome).ToList(),
                Categorias = bancoDados.Categorias
                    .Where(c => c.Jogos.Any(j => j.Ativo))
                    .OrderBy(c => c.Nome).ToList()
            };
        }

        /// <summary>Prateleiras da página inicial. Só entram produtos com chave disponível, um por jogo em cada prateleira.</summary>
        public PaginaInicialViewModel MontarInicio()
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var n = PaginaInicialViewModel.ItensPorPrateleira;
            var comEstoque = Projetar(AVenda(), hoje).Where(p => p.Disponiveis > 0);

            // Busca alguns a mais e fica com o primeiro produto de cada jogo (o mesmo jogo pode estar em várias plataformas)
            static List<ProdutoVitrineViewModel> UmPorJogo(IEnumerable<ProdutoVitrineViewModel> lista, int quantos) =>
                lista.GroupBy(p => p.JogoId).Select(g => g.First()).Take(quantos).ToList();

            var destaques = comEstoque.Where(p => p.Destaque)
                .OrderByDescending(p => p.JogoId).ThenBy(p => p.PrecoAtual)
                .Take(20).ToList();
            var promocoes = comEstoque.Where(p => p.PrecoAtual < p.Preco)
                .OrderBy(p => p.PrecoAtual / p.Preco).ThenBy(p => p.Titulo)   // maior desconto primeiro
                .Take(n * 4).ToList();
            var maisVendidos = comEstoque.Where(p => p.Vendidos > 0)
                .OrderByDescending(p => p.Vendidos).ThenBy(p => p.Titulo)
                .Take(n * 4).ToList();
            var lancamentos = comEstoque.Where(p => p.DataLancamento != null && p.DataLancamento <= hoje)
                .OrderByDescending(p => p.DataLancamento).ThenBy(p => p.Titulo)
                .Take(n * 4).ToList();

            return new PaginaInicialViewModel
            {
                Destaques = UmPorJogo(destaques, 5),
                Promocoes = UmPorJogo(promocoes, n),
                MaisVendidos = UmPorJogo(maisVendidos, n),
                Lancamentos = UmPorJogo(lancamentos, n),
                Plataformas = bancoDados.Plataformas
                    .Where(p => p.Ativa)
                    .Select(p => new { Plataforma = p, Produtos = p.Produtos.Count(x => x.Ativo && x.Jogo.Ativo) })
                    .Where(x => x.Produtos > 0)
                    .OrderByDescending(x => x.Produtos).ThenBy(x => x.Plataforma.Nome)
                    .AsEnumerable()
                    .Select(x => (x.Plataforma, x.Produtos))
                    .ToList()
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
                    Slug = d.Jogo.Slug,
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

        /// <summary>Página do jogo: informações, produtos à venda e avaliações. Nulo se o jogo não está na loja.</summary>
        public PaginaJogoViewModel? MontarPaginaJogo(string slug, int? usuarioId)
        {
            var jogo = bancoDados.Jogos
                .Where(j => j.Slug == slug && j.Ativo)
                .Include(j => j.Categorias)
                .Include(j => j.Desenvolvedora)
                .Include(j => j.Publicadora)
                .AsSplitQuery()
                .FirstOrDefault();
            if (jogo == null)
            {
                return null;
            }

            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var avaliacoes = bancoDados.Avaliacoes
                .Where(a => a.JogoId == jogo.Id)
                .OrderByDescending(a => a.AtualizadaEm ?? a.CriadaEm)
                .Select(a => new AvaliacaoViewModel
                {
                    UsuarioId = a.UsuarioId,
                    Nome = a.Usuario.Name,
                    Foto = a.Usuario.Foto,
                    Nota = a.Nota,
                    Comentario = a.Comentario,
                    Data = a.AtualizadaEm ?? a.CriadaEm,
                    Editada = a.AtualizadaEm != null
                })
                .ToList();

            var pagina = new PaginaJogoViewModel
            {
                Jogo = jogo,
                Produtos = Projetar(bancoDados.Produtos
                    .Where(p => p.JogoId == jogo.Id && p.Ativo && p.Plataforma.Ativa)
                    .OrderBy(p => p.Plataforma.Nome).ThenBy(p => p.Edicao), hoje).ToList(),
                Avaliacoes = avaliacoes,
                Imagens = bancoDados.JogoImagens.Where(i => i.JogoId == jogo.Id).OrderBy(i => i.Ordem).ThenBy(i => i.Id).ToList(),
                Logado = usuarioId.HasValue
            };

            if (usuarioId.HasValue)
            {
                pagina.MinhaAvaliacao = avaliacoes.FirstOrDefault(a => a.UsuarioId == usuarioId);
                pagina.PodeAvaliar = ComprouOJogo(usuarioId.Value, jogo.Id);
                pagina.NaListaDesejos = bancoDados.ListaDesejos.Any(d => d.UsuarioId == usuarioId && d.JogoId == jogo.Id);
            }
            return pagina;
        }

        /// <summary>Só avalia quem tem um pedido pago com algum produto do jogo.</summary>
        public bool ComprouOJogo(int usuarioId, int jogoId) =>
            bancoDados.PedidoItens.Any(i => i.Pedido.UsuarioId == usuarioId && i.Pedido.Status == StatusPedido.Pago
                                            && i.Produto.JogoId == jogoId);

        private IQueryable<ProdutoVitrineViewModel> Projetar(IQueryable<Produto> consulta, DateOnly hoje)
        {
            return consulta
                .Select(p => new ProdutoVitrineViewModel
                {
                    ProdutoId = p.Id,
                    JogoId = p.JogoId,
                    Slug = p.Jogo.Slug,
                    Titulo = p.Jogo.Titulo,
                    Capa = p.Jogo.Capa,
                    Plataforma = p.Plataforma.Nome,
                    Edicao = p.Edicao,
                    Categorias = p.Jogo.Categorias.OrderBy(c => c.Nome).Select(c => c.Nome).ToList(),
                    Desenvolvedora = p.Jogo.Desenvolvedora != null ? p.Jogo.Desenvolvedora.Nome : null,
                    DataLancamento = p.Jogo.DataLancamento,
                    Destaque = p.Jogo.Destaque,
                    Vendidos = bancoDados.PedidoItens
                        .Where(i => i.ProdutoId == p.Id && i.Pedido.Status == StatusPedido.Pago)
                        .Sum(i => (int?)i.Quantidade) ?? 0,
                    Preco = p.Preco,
                    PrecoAtual = p.PrecoPromocional != null && p.PrecoPromocional < p.Preco
                                 && (p.PromocaoAte == null || p.PromocaoAte >= hoje)
                        ? p.PrecoPromocional.Value
                        : p.Preco,
                    Disponiveis = p.Chaves.Count(c => c.Status == StatusChave.Disponivel),
                    MediaAvaliacoes = p.Jogo.Avaliacoes.Average(a => (double?)a.Nota),
                    TotalAvaliacoes = p.Jogo.Avaliacoes.Count()
                });
        }
    }
}
