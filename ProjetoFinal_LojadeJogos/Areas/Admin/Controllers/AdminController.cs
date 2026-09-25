using Microsoft.AspNetCore.Mvc;

namespace ProjetoFinal_LojadeJogos.Areas.Admin.Controllers
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
        public string SalvaArquivo(IFormFile arquivo)
        {
            // VERIFICAR SE O ARQUIVO É VÁLIDO
            if (arquivo == null)
            {
                return string.Empty;
            }
            //ARMAZENAR O ARQUIVO NO SERVIDOR WEB
            var nomeArquivo = $"{Path.GetRandomFileName()}{Path.GetExtension(arquivo.FileName)}";
            var pastaArquivo = Path.Combine(servidorWeb.WebRootPath, "arquivos");
            var localArquivo = Path.Combine(pastaArquivo, nomeArquivo);
            var dadosArquivo = new FileStream(localArquivo, FileMode.Create);
            arquivo.CopyTo(dadosArquivo);
            return nomeArquivo;

        }

        public bool ExcluiArquivo(string nomeArquivo)
        {
            //verifica o nome do arquivo
            if (string.IsNullOrWhiteSpace(nomeArquivo))
            {
                return false;
            }

            //remover o arquivo no servidor web
            var pastaArquivo = Path.Combine(servidorWeb.WebRootPath, "arquivos");
            var localArquivo = Path.Combine(pastaArquivo, nomeArquivo);
            System.IO.File.Delete(localArquivo);
            return true;
        }

    }


}
