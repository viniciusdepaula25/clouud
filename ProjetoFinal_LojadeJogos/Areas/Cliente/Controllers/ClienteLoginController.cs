using Microsoft.AspNetCore.Mvc;

namespace ProjetoFinal_LojadeJogos.Areas.Cliente.Controllers
{
    [Area("Cliente")]
    public abstract class ClienteLoginController : Controller
    {
        IWebHostEnvironment servidorWeb;
        public ClienteLoginController(IWebHostEnvironment webHostEnvironment)
        {
            servidorWeb = webHostEnvironment;
        }
    }
}
