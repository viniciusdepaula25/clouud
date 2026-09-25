using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using System.Diagnostics;

namespace Clouud.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; 
        
        private readonly BancoDados bancoDados;

        public HomeController(ILogger<HomeController> logger, BancoDados bancoDados)
        {
            _logger = logger;
            this.bancoDados = bancoDados;
        }

        public IActionResult Index(string? busca)
        {
            //lista todos os jogos (ou só os que batem com a busca)
            var consulta = bancoDados.Jogos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                consulta = consulta.Where(e => EF.Functions.ILike(e.Nome, $"%{busca}%"));
            }
            var jogos = consulta.ToList();
            //envia a lista de usuarios para a view
            return View(jogos);
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
