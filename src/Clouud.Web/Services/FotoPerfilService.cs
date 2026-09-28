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

        /// <summary>Valida e grava a imagem (ver <see cref="ImagemUpload"/>).</summary>
        public (string? Foto, string? Erro) Salvar(IFormFile? arquivo) =>
            ImagemUpload.Salvar(arquivo, ambiente.WebRootPath, Pasta, TamanhoMaximo);

        /// <summary>Apaga o arquivo da foto. Só apaga dentro de uploads/perfis.</summary>
        public void Excluir(string? foto) => ImagemUpload.Excluir(foto, ambiente.WebRootPath, Pasta);
    }
}
