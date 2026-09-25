using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ProjetoFinal_LojadeJogos.Areas.Admin.Controllers
{
    //[Authorize(Roles = "Administrador,Cliente")]
    public class HomeController : AdminController 
    {
        public HomeController(IWebHostEnvironment webHostEnvironment) : base(webHostEnvironment)
        { 
        }    
        public IActionResult Index()
        {
            return View();
        }
    }
}
