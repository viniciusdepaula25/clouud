using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    /// <summary>Lista de desejos: jogos que o cliente quer acompanhar.</summary>
    [Authorize(Roles = "Admin,Cliente")]
    public class ListaDesejosController : ClienteLoginController
    {
        private readonly ListaDesejosService desejos;
        private readonly CatalogoService catalogo;

        public ListaDesejosController(IWebHostEnvironment webHostEnvironment, ListaDesejosService desejos, CatalogoService catalogo)
            : base(webHostEnvironment)
        {
            this.desejos = desejos;
            this.catalogo = catalogo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(catalogo.MontarListaDesejos(UsuarioId));
        }

        /// <summary>Coração da vitrine: põe ou tira o jogo da lista e volta para a mesma página.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Alternar(int jogoId, string? voltarPara)
        {
            var naLista = desejos.Alternar(UsuarioId, jogoId);
            if (naLista == null)
            {
                TempData["Erro"] = "Este jogo não está mais na loja.";
            }
            return Url.IsLocalUrl(voltarPara) ? LocalRedirect(voltarPara) : RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remover(int jogoId)
        {
            desejos.Remover(UsuarioId, jogoId);
            TempData["Mensagem"] = "Jogo removido da lista de desejos.";
            return RedirectToAction("Index");
        }
    }
}
