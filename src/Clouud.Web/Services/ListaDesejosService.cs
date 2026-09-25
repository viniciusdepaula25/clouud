using Clouud.Web.Data;
using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Services
{
    public class ListaDesejosService
    {
        private readonly BancoDados bancoDados;

        public ListaDesejosService(BancoDados bancoDados)
        {
            this.bancoDados = bancoDados;
        }

        /// <summary>Ids dos jogos na lista do usuário (para marcar o coração na vitrine).</summary>
        public HashSet<int> JogosDoUsuario(int usuarioId)
        {
            return bancoDados.ListaDesejos.Where(d => d.UsuarioId == usuarioId).Select(d => d.JogoId).ToHashSet();
        }

        public int Contar(int usuarioId)
        {
            return bancoDados.ListaDesejos.Count(d => d.UsuarioId == usuarioId);
        }

        /// <summary>Põe o jogo na lista ou tira, se já estiver. Devolve true se ficou na lista.</summary>
        public bool? Alternar(int usuarioId, int jogoId)
        {
            var existente = bancoDados.ListaDesejos.FirstOrDefault(d => d.UsuarioId == usuarioId && d.JogoId == jogoId);
            if (existente != null)
            {
                bancoDados.ListaDesejos.Remove(existente);
                bancoDados.SaveChanges();
                return false;
            }

            if (!bancoDados.Jogos.Any(j => j.Id == jogoId && j.Ativo))
            {
                return null;
            }

            bancoDados.ListaDesejos.Add(new ListaDesejo { UsuarioId = usuarioId, JogoId = jogoId });
            try
            {
                bancoDados.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Dois cliques ao mesmo tempo: o outro já adicionou
                bancoDados.ChangeTracker.Clear();
            }
            return true;
        }

        public void Remover(int usuarioId, int jogoId)
        {
            bancoDados.ListaDesejos.Where(d => d.UsuarioId == usuarioId && d.JogoId == jogoId).ExecuteDelete();
        }
    }
}
