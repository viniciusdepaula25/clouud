using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    /// <summary>
    /// Endereço antigo da vitrine do cliente (/Cliente/Home). A loja agora é uma só para visitante e cliente,
    /// então ele só redireciona, mantendo a busca e os filtros.
    /// </summary>
    [Area("Cliente")]
    public class HomeController : Controller
    {
        public IActionResult Index() => Redirect("/loja" + Request.QueryString);
    }
}
