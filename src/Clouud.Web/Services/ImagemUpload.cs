namespace Clouud.Web.Services
{
    /// <summary>
    /// Gravação de imagens enviadas pelo site (fotos de perfil, galeria dos jogos). O tipo é conferido pelo
    /// conteúdo do arquivo (os primeiros bytes), não pelo nome: um arquivo qualquer renomeado para .png é
    /// recusado. SVG não é aceito porque pode conter scripts.
    /// </summary>
    public static class ImagemUpload
    {
        public const string FormatosAceitos = "JPG, PNG, GIF ou WebP";
        public const string AcceptHtml = "image/jpeg,image/png,image/gif,image/webp";

        /// <summary>Grava em wwwroot/uploads/{subpasta} com nome aleatório. Devolve "subpasta/arquivo.ext" ou o erro.</summary>
        public static (string? Caminho, string? Erro) Salvar(IFormFile? arquivo, string webRoot, string subpasta, long tamanhoMaximo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                return (null, "Escolha uma imagem.");
            }
            if (arquivo.Length > tamanhoMaximo)
            {
                return (null, $"A imagem pode ter no máximo {tamanhoMaximo / (1024 * 1024)} MB.");
            }

            var cabecalho = new byte[12];
            using (var leitura = arquivo.OpenReadStream())
            {
                leitura.ReadAtLeast(cabecalho, cabecalho.Length, throwOnEndOfStream: false);
            }
            var extensao = DetectarExtensao(cabecalho);
            if (extensao == null)
            {
                return (null, $"Formato não aceito. Envie uma imagem {FormatosAceitos}.");
            }

            var pasta = Path.Combine(webRoot, "uploads", subpasta);
            Directory.CreateDirectory(pasta);
            var nome = Path.GetRandomFileName().Replace(".", "") + extensao;
            using (var destino = new FileStream(Path.Combine(pasta, nome), FileMode.CreateNew))
            {
                arquivo.CopyTo(destino);
            }
            return ($"{subpasta}/{nome}", null);
        }

        /// <summary>Apaga um arquivo gravado por <see cref="Salvar"/>. Só apaga dentro de uploads/{subpasta}.</summary>
        public static void Excluir(string? caminho, string webRoot, string subpasta)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !caminho.StartsWith(subpasta + "/"))
            {
                return;
            }
            var arquivo = Path.Combine(webRoot, "uploads", subpasta, Path.GetFileName(caminho));
            if (File.Exists(arquivo))
            {
                File.Delete(arquivo);
            }
        }

        /// <summary>Reconhece o formato pela assinatura do arquivo.</summary>
        public static string? DetectarExtensao(byte[] b)
        {
            if (b.Length < 12) return null;
            if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return ".gif";
            if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }
    }
}
