using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;


namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Cliente")]
    public class ClientesController : AdminController
    {
        BancoDados bancoDados;
        
        public ClientesController(IWebHostEnvironment webHostEnvironment) : base(webHostEnvironment) 
        {
        }
        public IActionResult Index() 
        { 
         return View();
        }
        
        //[HttpGet]
        //public IActionResult Inclui()
        //{
        //    var idUsuario = Convert
        //        .ToInt32(HttpContext.User.Claims
        //        .FirstOrDefault(e => e.Type == ClaimTypes.NameIdentifier).Value);

        //    var perfilUsaurio = Convert
        //        .ToInt32(HttpContext.User.Claims
        //        .FirstOrDefault(e => e.Type == ClaimTypes.Role).Value).ToString();

        //    bancoDados = new BancoDados();
        //    var cliente = new Cliente();
        //    //cliente.Id = idUsuario;
        //    //var usuarios = bancoDados.Usuarios.Where(e => e.Id == idUsuario).ToList();
        //    var usuarios = bancoDados.Usuarios.ToList();
        //    ViewBag.Usuarios = new SelectList(usuarios, "Id", "Email", cliente.Id);
        //    return View(cliente);
        //}
    }   
}
