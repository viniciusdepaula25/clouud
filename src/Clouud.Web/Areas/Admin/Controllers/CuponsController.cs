using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clouud.Web.Areas.Admin.Controllers
{
    /// <summary>Cupons de desconto.</summary>
    [Authorize(Roles = "Admin")]
    public class CuponsController : AdminController
    {
        private readonly BancoDados bancoDados;

        public CuponsController(IWebHostEnvironment webHostEnvironment, BancoDados bancoDados) : base(webHostEnvironment)
        {
            this.bancoDados = bancoDados;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cupons = bancoDados.Cupons
                .OrderByDescending(c => c.Ativo).ThenBy(c => c.Codigo)
                .ToList();
            ViewData["Usos"] = bancoDados.Pedidos
                .Where(p => p.CupomId != null && CupomService.StatusQueUsam.Contains(p.Status))
                .GroupBy(p => p.CupomId!.Value)
                .Select(g => new { g.Key, Usos = g.Count(), Descontos = g.Sum(p => p.Desconto) })
                .ToDictionary(x => x.Key, x => (x.Usos, x.Descontos));
            return View(cupons);
        }

        [HttpGet]
        public IActionResult Inclui()
        {
            return View(new Cupom());
        }

        /// <summary>Só estes campos vêm do formulário (nada de Pedidos, CriadoEm... num POST forjado).</summary>
        private const string CamposDoFormulario =
            "Codigo,Descricao,Tipo,Valor,PedidoMinimo,ValidoDe,ValidoAte,LimiteUsos,LimitePorCliente,Ativo";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inclui([Bind(CamposDoFormulario)] Cupom cupom)
        {
            cupom.Id = 0; // o Id é do banco, nunca do formulário
            Normalizar(cupom);
            ValidarCodigo(cupom);
            if (!ModelState.IsValid)
            {
                return View(cupom);
            }

            cupom.CriadoEm = DateTime.UtcNow;
            bancoDados.Cupons.Add(cupom);
            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Cupom {cupom.Codigo} cadastrado.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Altera(int id)
        {
            var cupom = bancoDados.Cupons.FirstOrDefault(c => c.Id == id);
            return cupom == null ? NotFound() : View(cupom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Altera([Bind("Id," + CamposDoFormulario)] Cupom dados)
        {
            var cupom = bancoDados.Cupons.FirstOrDefault(c => c.Id == dados.Id);
            if (cupom == null)
            {
                return NotFound();
            }

            Normalizar(dados);
            ValidarCodigo(dados);
            if (!ModelState.IsValid)
            {
                return View(dados);
            }

            // Os pedidos já feitos guardam o desconto que receberam; mudar o cupom só afeta os próximos
            cupom.Codigo = dados.Codigo;
            cupom.Descricao = dados.Descricao;
            cupom.Tipo = dados.Tipo;
            cupom.Valor = dados.Valor;
            cupom.PedidoMinimo = dados.PedidoMinimo;
            cupom.ValidoDe = dados.ValidoDe;
            cupom.ValidoAte = dados.ValidoAte;
            cupom.LimiteUsos = dados.LimiteUsos;
            cupom.LimitePorCliente = dados.LimitePorCliente;
            cupom.Ativo = dados.Ativo;
            bancoDados.SaveChanges();

            TempData["Mensagem"] = $"Cupom {cupom.Codigo} alterado.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Exclui(int id)
        {
            var cupom = bancoDados.Cupons.FirstOrDefault(c => c.Id == id);
            return cupom == null ? NotFound() : View(cupom);
        }

        [HttpPost, ActionName("Exclui")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmaExclusao(int id)
        {
            var cupom = bancoDados.Cupons.FirstOrDefault(c => c.Id == id);
            if (cupom == null)
            {
                return NotFound();
            }

            // Cupom que já foi usado fica no histórico dos pedidos: o caminho é desativar
            if (bancoDados.Pedidos.Any(p => p.CupomId == id))
            {
                ModelState.AddModelError(string.Empty,
                    "Este cupom já foi usado em pedidos e não pode ser excluído. Desmarque \"Ativo\" para que ninguém mais use.");
                return View(cupom);
            }

            bancoDados.Cupons.Remove(cupom);
            bancoDados.SaveChanges();
            TempData["Mensagem"] = $"Cupom {cupom.Codigo} excluído.";
            return RedirectToAction("Index");
        }

        private static void Normalizar(Cupom cupom)
        {
            cupom.Codigo = CupomService.Normalizar(cupom.Codigo);
            cupom.Descricao = string.IsNullOrWhiteSpace(cupom.Descricao) ? null : cupom.Descricao.Trim();
        }

        private void ValidarCodigo(Cupom cupom)
        {
            if (bancoDados.Cupons.Any(c => c.Codigo == cupom.Codigo && c.Id != cupom.Id))
            {
                ModelState.AddModelError(nameof(cupom.Codigo), "Já existe um cupom com este código");
            }
        }
    }
}
