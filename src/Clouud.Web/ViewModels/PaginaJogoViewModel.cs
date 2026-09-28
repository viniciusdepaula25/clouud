using Clouud.Web.Models;

namespace Clouud.Web.ViewModels
{
    /// <summary>Página pública de um jogo (/jogo/{slug}).</summary>
    public class PaginaJogoViewModel
    {
        public Jogo Jogo { get; set; } = null!;
        public List<ProdutoVitrineViewModel> Produtos { get; set; } = new();
        public List<AvaliacaoViewModel> Avaliacoes { get; set; } = new();
        public List<JogoImagem> Imagens { get; set; } = new();

        public bool Logado { get; set; }
        public bool PodeAvaliar { get; set; }
        public bool NaListaDesejos { get; set; }
        public AvaliacaoViewModel? MinhaAvaliacao { get; set; }

        public double? Media => Avaliacoes.Count == 0 ? null : Avaliacoes.Average(a => a.Nota);

        /// <summary>Quantas avaliações de cada nota (índice 1 a 5).</summary>
        public int QuantidadeComNota(int nota) => Avaliacoes.Count(a => a.Nota == nota);
    }

    public class AvaliacaoViewModel
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Foto { get; set; }
        public int Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime Data { get; set; }
        public bool Editada { get; set; }
    }

    /// <summary>Formulário "avaliar este jogo".</summary>
    public class AvaliarViewModel
    {
        public int Nota { get; set; }
        public string? Comentario { get; set; }
    }
}
