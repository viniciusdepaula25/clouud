using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.ViewModels;
using System.Diagnostics;

namespace Clouud.Web.Areas.Cliente.Controllers
{
    [Authorize(Roles = "Admin,Cliente")]
    public class HomeController : ClienteLoginController
    {
        private readonly BancoDados bancoDados;

        public HomeController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
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
