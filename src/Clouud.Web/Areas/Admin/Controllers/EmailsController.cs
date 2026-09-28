using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Registro dos e-mails automáticos: o que foi enviado, o que está na fila e o que falhou.</summary>
    [Authorize(Roles = "Admin")]
    public class EmailsController : AdminController
    {
        private const int LimiteListagem = 200;
        private readonly BancoDados bancoDados;

        public EmailsController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index(TipoEmail? tipo, string? situacao, string? busca)
        {
            var consulta = bancoDados.Emails.AsQueryable();
            if (tipo.HasValue)
            {
                consulta = consulta.Where(e => e.Tipo == tipo);
            }
            consulta = situacao switch
            {
                "enviados" => consulta.Where(e => e.EnviadoEm != null),
                "pendentes" => consulta.Where(e => e.EnviadoEm == null && e.Tentativas < Email.MaximoTentativas),
                "falhas" => consulta.Where(e => e.EnviadoEm == null && e.Tentativas >= Email.MaximoTentativas),
                _ => consulta
            };
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = $"%{busca.Trim()}%";
                consulta = consulta.Where(e => EF.Functions.ILike(e.Para, termo) || EF.Functions.ILike(e.Assunto, termo));
            }

            ViewData["Tipo"] = tipo;
            ViewData["Situacao"] = situacao;
            ViewData["Busca"] = busca;
            ViewData["Pendentes"] = bancoDados.Emails.Count(e => e.EnviadoEm == null && e.Tentativas < Email.MaximoTentativas);
            ViewData["Falhas"] = bancoDados.Emails.Count(e => e.EnviadoEm == null && e.Tentativas >= Email.MaximoTentativas);
            return View(consulta.OrderByDescending(e => e.Id).Take(LimiteListagem).ToList());
        }
    }
}
