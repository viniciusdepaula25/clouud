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

        //Metodos de Manipulação de Arquivos
        // protected: métodos auxiliares, não podem ser acessados como página (action)
        protected string SalvaArquivo(IFormFile? arquivo)
        {
            // VERIFICAR SE O ARQUIVO É VÁLIDO
            if (arquivo == null)
            {
                return string.Empty;
            }
            //ARMAZENAR O ARQUIVO NO SERVIDOR WEB
            var nomeArquivo = $"{Path.GetRandomFileName()}{Path.GetExtension(arquivo.FileName)}";
            var pastaArquivo = Path.Combine(servidorWeb.WebRootPath, "uploads");
            var localArquivo = Path.Combine(pastaArquivo, nomeArquivo);
            using var dadosArquivo = new FileStream(localArquivo, FileMode.Create);
            arquivo.CopyTo(dadosArquivo);
            return nomeArquivo;

        }

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
