using System.Security.Claims;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : AdminController
    {
        private readonly BancoDados bancoDados;
        private readonly SenhaService senhas;
        private readonly FotoPerfilService fotos;

        public UsuariosController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados, SenhaService senhas,
            FotoPerfilService fotos) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
            this.senhas = senhas;
            this.fotos = fotos;
        }

        [HttpGet]
        public IActionResult Index()
        {
            //lista todos os usuarios
            var usuarios = bancoDados.Usuarios.OrderBy(e => e.Name).ToList();
            return View(usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string? busca)
        {
            //lista os usuarios realizando a busca por nome ou e-mail
            var consulta = bancoDados.Usuarios.AsQueryable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = $"%{busca.Trim()}%";
                consulta = consulta.Where(e => EF.Functions.ILike(e.Name, termo) || EF.Functions.ILike(e.Email, termo));
            }
            ViewData["Busca"] = busca;
            return View(consulta.OrderBy(e => e.Name).ToList());
        }

        [HttpGet]
        public IActionResult Inclui()
        {
            return View(new UsuarioFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui(UsuarioFormViewModel form)
        {
            var email = form.Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(form.Senha))
            {
                ModelState.AddModelError(nameof(form.Senha), "Senha obrigatória");
            }
            if (bancoDados.Usuarios.Any(e => e.Email == email))
            {
                ModelState.AddModelError(nameof(form.Email), "Já existe uma conta com este e-mail");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var usuario = new Usuario
            {
                Name = form.Nome.Trim(),
                Email = email,
                Perfil = form.Perfil
            };
            usuario.Senha = senhas.GerarHash(usuario, form.Senha!); // salva só o hash
            bancoDados.Usuarios.Add(usuario);
            bancoDados.SaveChanges();

            TempData["Mensagem"] = $"Usuário {usuario.Name} cadastrado.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(new UsuarioFormViewModel
            {
                ID = usuario.ID,
                Nome = usuario.Name,
                Email = usuario.Email,
                Perfil = usuario.Perfil
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera(UsuarioFormViewModel form)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == form.ID);
            if (usuario == null)
            {
                return NotFound();
            }

            var email = form.Email.Trim().ToLowerInvariant();
            if (bancoDados.Usuarios.Any(e => e.Email == email && e.ID != usuario.ID))
            {
                ModelState.AddModelError(nameof(form.Email), "Já existe uma conta com este e-mail");
            }

            var deixaDeSerAdmin = usuario.Perfil == PerfilUsuario.Admin && form.Perfil != PerfilUsuario.Admin;
            if (deixaDeSerAdmin && usuario.ID == UsuarioLogadoId())
            {
                ModelState.AddModelError(nameof(form.Perfil), "Você não pode tirar o seu próprio perfil de administrador");
            }
            else if (deixaDeSerAdmin && UltimoAdmin(usuario.ID))
            {
                ModelState.AddModelError(nameof(form.Perfil), "Este é o único administrador; cadastre outro antes de alterar o perfil");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            usuario.Name = form.Nome.Trim();
            usuario.Email = email;
            usuario.Perfil = form.Perfil;
            if (!string.IsNullOrWhiteSpace(form.Senha))
            {
                usuario.Senha = senhas.GerarHash(usuario, form.Senha); // senha nova: gera o hash
            }

            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Usuário {usuario.Name} alterado.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Exibe(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpPost, ActionName("Exclui")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmaExclusao(int id)
        {
            var usuario = bancoDados.Usuarios.FirstOrDefault(e => e.ID == id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (usuario.ID == UsuarioLogadoId())
            {
                ModelState.AddModelError(string.Empty, "Você não pode excluir a sua própria conta");
                return View(usuario);
            }
            if (usuario.Perfil == PerfilUsuario.Admin && UltimoAdmin(usuario.ID))
            {
                ModelState.AddModelError(string.Empty, "Este é o único administrador e não pode ser excluído");
                return View(usuario);
            }

            bancoDados.Usuarios.Remove(usuario);

            try
            {
                bancoDados.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Este usuário tem pedidos registrados e não pode ser excluído");
                return View(usuario);
            }
            fotos.Excluir(usuario.Foto);

            TempData["Mensagem"] = $"Usuário {usuario.Name} excluído.";
            return RedirectToAction("Index");
        }

        private int UsuarioLogadoId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        /// <summary>True se não existe outro administrador além do usuário informado.</summary>
        private bool UltimoAdmin(int usuarioId)
        {
            return !bancoDados.Usuarios.Any(e => e.Perfil == PerfilUsuario.Admin && e.ID != usuarioId);
        }
    }
}
