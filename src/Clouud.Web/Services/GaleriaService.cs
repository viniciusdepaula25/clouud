using Clouud.Web.Data;
using Clouud.Web.Models;

namespace Clouud.Web.Services
{
    /// <summary>Resultado do envio de várias imagens de uma vez.</summary>
    public class ResultadoEnvioGaleria
    {
        public int Adicionadas { get; set; }
        public List<string> Recusadas { get; } = new();
    }

    /// <summary>Galeria de imagens dos jogos, guardada em wwwroot/uploads/galeria.</summary>
    public class GaleriaService
    {
        private const string Pasta = "galeria";

        private readonly BancoDados bancoDados;
        private readonly IWebHostEnvironment ambiente;

        public GaleriaService(BancoDados bancoDados, IWebHostEnvironment ambiente)
        {
            this.bancoDados = bancoDados;
            this.ambiente = ambiente;
        }

        public static string Url(JogoImagem imagem) => "/uploads/" + imagem.Arquivo;

        public List<JogoImagem> Listar(int jogoId) =>
            bancoDados.JogoImagens.Where(i => i.JogoId == jogoId).OrderBy(i => i.Ordem).ThenBy(i => i.Id).ToList();

        /// <summary>Grava as imagens válidas no fim da galeria; as outras voltam com o motivo.</summary>
        public ResultadoEnvioGaleria Enviar(int jogoId, IEnumerable<IFormFile> arquivos)
        {
            var resultado = new ResultadoEnvioGaleria();
            var existentes = bancoDados.JogoImagens.Where(i => i.JogoId == jogoId).ToList();
            var ordem = existentes.Count == 0 ? 0 : existentes.Max(i => i.Ordem) + 1;
            var total = existentes.Count;

            foreach (var arquivo in arquivos)
            {
                if (total >= JogoImagem.MaximoPorJogo)
                {
                    resultado.Recusadas.Add($"{arquivo.FileName}: a galeria já tem o máximo de {JogoImagem.MaximoPorJogo} imagens.");
                    continue;
                }
                var (caminho, erro) = ImagemUpload.Salvar(arquivo, ambiente.WebRootPath, Pasta, JogoImagem.TamanhoMaximo);
                if (erro != null)
                {
                    resultado.Recusadas.Add($"{arquivo.FileName}: {erro}");
                    continue;
                }
                bancoDados.JogoImagens.Add(new JogoImagem { JogoId = jogoId, Arquivo = caminho!, Ordem = ordem++ });
                total++;
                resultado.Adicionadas++;
            }
            bancoDados.SaveChanges();
            return resultado;
        }

        /// <summary>Troca a imagem de lugar com a vizinha (-1 = para a esquerda, +1 = para a direita).</summary>
        public void Mover(JogoImagem imagem, int direcao)
        {
            var lista = Listar(imagem.JogoId);
            // renumera 0, 1, 2... para a troca funcionar mesmo que a ordem tenha buracos
            for (var i = 0; i < lista.Count; i++)
            {
                lista[i].Ordem = i;
            }
            var posicao = lista.FindIndex(i => i.Id == imagem.Id);
            var destino = posicao + Math.Sign(direcao);
            if (posicao >= 0 && destino >= 0 && destino < lista.Count)
            {
                (lista[posicao].Ordem, lista[destino].Ordem) = (lista[destino].Ordem, lista[posicao].Ordem);
            }
            bancoDados.SaveChanges();
        }

        public void Excluir(JogoImagem imagem)
        {
            bancoDados.JogoImagens.Remove(imagem);
            bancoDados.SaveChanges();
            ImagemUpload.Excluir(imagem.Arquivo, ambiente.WebRootPath, Pasta);
        }

        /// <summary>Apaga os arquivos da galeria (depois de excluir o jogo; as linhas saem em cascata).</summary>
        public void ExcluirArquivos(IEnumerable<string> arquivos)
        {
            foreach (var arquivo in arquivos)
            {
                ImagemUpload.Excluir(arquivo, ambiente.WebRootPath, Pasta);
            }
        }
    }
}
