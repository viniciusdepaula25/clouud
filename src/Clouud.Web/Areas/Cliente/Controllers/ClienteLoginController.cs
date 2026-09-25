using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Cliente.Controllers
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
