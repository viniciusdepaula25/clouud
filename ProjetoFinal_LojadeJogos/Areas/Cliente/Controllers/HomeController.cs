using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProjetoFinal_LojadeJogos.Data;
using ProjetoFinal_LojadeJogos.Models;
using System.Diagnostics;

namespace ProjetoFinal_LojadeJogos.Areas.Cliente.Controllers
{
    //[Authorize(Roles = "Administrador,Cliente")]
    public class HomeController : ClienteLoginController
    {
        public HomeController(IWebHostEnvironment webHostEnvironment) : base(webHostEnvironment)
        {
        }

        BancoDados bancoDados;


        public IActionResult Index()
        {
            bancoDados = new BancoDados();
            //lista todos os usuarios
            var jogos = bancoDados.Jogos.ToList();
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
