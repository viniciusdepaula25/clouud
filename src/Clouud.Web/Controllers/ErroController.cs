using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Controllers
{
    /// <summary>Páginas de erro amigáveis (404, 429 "muitas tentativas"...), no lugar da tela em branco.</summary>
    [IgnoreAntiforgeryToken]
    public class ErroController : Controller
    {
        [Route("/erro/{codigo:int}")]
        public IActionResult Index(int codigo)
        {
            // Chamado de novo pelo UseStatusCodePagesWithReExecute: mantém o código original na resposta
            Response.StatusCode = codigo;
            ViewData["Codigo"] = codigo;
            ViewData["Original"] = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;
            return View();
        }
    }
}
