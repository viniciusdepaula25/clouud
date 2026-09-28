using Microsoft.AspNetCore.Mvc;
using Clouud.Web.Services;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace Clouud.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; 
        
        private readonly CatalogoService catalogo;
        private readonly ListaDesejosService desejos;

        public HomeController(ILogger<HomeController> logger, CatalogoService catalogo, ListaDesejosService desejos)
        {
            _logger = logger;
            this.catalogo = catalogo;
            this.desejos = desejos;
        }

        public IActionResult Index(string? busca, string? plataforma, string? categoria, bool promocoes = false)
        {
            // produtos à venda, com os filtros de busca, plataforma, categoria e promoção
            var vitrine = catalogo.MontarVitrine(busca, plataforma, categoria, promocoes);
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            {
                vitrine.JogosDesejados = desejos.JogosDoUsuario(usuarioId);
            }
            return View(vitrine);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
