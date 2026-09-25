using System.Security.Claims;
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

        /// <summary>Id do usuário logado (gravado no cookie de login).</summary>
        protected int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
