using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public abstract class AdminController : Controller
    {
        IWebHostEnvironment servidorWeb;
        public AdminController(IWebHostEnvironment webHostEnvironment)
        {
            servidorWeb = webHostEnvironment;
        }

        /// <summary>Pasta raiz dos arquivos públicos (wwwroot).</summary>
        protected string WebRoot => servidorWeb.WebRootPath;

        // Imagens novas são gravadas com Services/ImagemUpload (confere o conteúdo do arquivo).
        // Este método só apaga arquivos antigos, que ficavam direto em wwwroot/uploads.
        // protected: método auxiliar, não pode ser acessado como página (action)
        protected bool ExcluiArquivo(string? nomeArquivo)
        {
            //verifica o nome do arquivo
            if (string.IsNullOrWhiteSpace(nomeArquivo))
            {
                return false;
            }

            //remover o arquivo no servidor web
            var pastaArquivo = Path.Combine(servidorWeb.WebRootPath, "uploads");
            // GetFileName impede caminhos como "../appsettings.json" (só apaga dentro de uploads)
            var localArquivo = Path.Combine(pastaArquivo, Path.GetFileName(nomeArquivo));
            System.IO.File.Delete(localArquivo);
            return true;
        }

    }


}
