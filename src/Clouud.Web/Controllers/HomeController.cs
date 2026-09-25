using Microsoft.AspNetCore.Mvc;
using Clouud.Web.Services;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using System.Diagnostics;

namespace Clouud.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; 
        
        private readonly CatalogoService catalogo;

        public HomeController(ILogger<HomeController> logger, CatalogoService catalogo)
        {
            _logger = logger;
            this.catalogo = catalogo;
        }

        public IActionResult Index(string? busca, string? plataforma, string? categoria, bool promocoes = false)
        {
            // produtos à venda, com os filtros de busca, plataforma, categoria e promoção
            var vitrine = catalogo.MontarVitrine(busca, plataforma, categoria, promocoes);
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
