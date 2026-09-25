using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
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
