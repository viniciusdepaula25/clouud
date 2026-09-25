using Clouud.Web.Data;

namespace Clouud.Web.Services
{
    /// <summary>
    /// Foto de perfil do usuário, guardada em wwwroot/uploads/perfis. A coluna usuarios.foto guarda o caminho
    /// dentro de uploads (ex.: "perfis/abc123.png").
    /// </summary>
    public class FotoPerfilService
    {
        public const long TamanhoMaximo = 2 * 1024 * 1024;
        public const string FotoPadrao = "/img/perfil.jpg";
        private const string Pasta = "perfis";

        private readonly BancoDados bancoDados;
        private readonly IWebHostEnvironment ambiente;

        public FotoPerfilService(BancoDados bancoDados, IWebHostEnvironment ambiente)
        {
            this.bancoDados = bancoDados;
            this.ambiente = ambiente;
        }

        /// <summary>Endereço da foto para usar no &lt;img&gt;; sem foto, a imagem padrão.</summary>
        public static string Url(string? foto) =>
            string.IsNullOrWhiteSpace(foto) ? FotoPadrao : "/uploads/" + foto.TrimStart('/');

        public string UrlDoUsuario(int usuarioId) =>
            Url(bancoDados.Usuarios.Where(u => u.ID == usuarioId).Select(u => u.Foto).FirstOrDefault());

        /// <summary>
        /// Valida e grava a imagem. O tipo é conferido pelo conteúdo do arquivo (não pelo nome), então um
        /// arquivo qualquer renomeado para .png é recusado. SVG não é aceito porque pode conter scripts.
        /// </summary>
        public (string? Foto, string? Erro) Salvar(IFormFile? arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                return (null, "Escolha uma imagem.");
            }
            if (arquivo.Length > TamanhoMaximo)
            {
                return (null, "A imagem pode ter no máximo 2 MB.");
            }

            var cabecalho = new byte[12];
            using (var leitura = arquivo.OpenReadStream())
            {
                leitura.ReadAtLeast(cabecalho, cabecalho.Length, throwOnEndOfStream: false);
            }
            var extensao = DetectarExtensao(cabecalho);
            if (extensao == null)
            {
                return (null, "Formato não aceito. Envie uma imagem JPG, PNG, GIF ou WebP.");
            }

            var pasta = Path.Combine(ambiente.WebRootPath, "uploads", Pasta);
            Directory.CreateDirectory(pasta);
            var nome = Path.GetRandomFileName().Replace(".", "") + extensao;
            using (var destino = new FileStream(Path.Combine(pasta, nome), FileMode.CreateNew))
            {
                arquivo.CopyTo(destino);
            }
            return ($"{Pasta}/{nome}", null);
        }

        /// <summary>Apaga o arquivo da foto. Só apaga dentro de uploads/perfis.</summary>
        public void Excluir(string? foto)
        {
            if (string.IsNullOrWhiteSpace(foto) || !foto.StartsWith(Pasta + "/"))
            {
                return;
            }
            var caminho = Path.Combine(ambiente.WebRootPath, "uploads", Pasta, Path.GetFileName(foto));
            if (File.Exists(caminho))
            {
                File.Delete(caminho);
            }
        }

        /// <summary>Reconhece o formato pelos primeiros bytes do arquivo ("assinatura").</summary>
        private static string? DetectarExtensao(byte[] b)
        {
            if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return ".gif";
            if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }
    }
}
