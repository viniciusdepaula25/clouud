using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Tela inicial do admin: vendas do período e o que precisa de atenção.</summary>
    [Authorize(Roles = "Admin")]
    public class HomeController : AdminController
    {
        private readonly PainelService painel;

        public HomeController(IWebHostEnvironment webHostEnvironment, PainelService painel) : base(webHostEnvironment)
        {
            this.painel = painel;
        }

        [HttpGet]
        public IActionResult Index(int dias = 30)
        {
            return View(painel.Montar(dias));
        }
    }
}
