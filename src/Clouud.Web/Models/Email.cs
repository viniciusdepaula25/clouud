using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>
    /// Fila de e-mails. As telas só gravam aqui (junto com o resto da operação); o serviço EnvioEmails
    /// manda em segundo plano, tenta de novo se o servidor falhar e apaga o conteúdo depois de enviar
    /// (links de senha e chaves não ficam guardados no banco).
    /// </summary>
    [Table("emails")]
    [Index(nameof(EnviadoEm), nameof(Id))]
    public class Email
    {
        public const int MaximoTentativas = 5;

        [Key]
        public int Id { get; set; }

        /// <summary>Destinatário com conta (nulo se a conta for excluída).</summary>
        public int? UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required, StringLength(100)]
        public string Para { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Assunto { get; set; } = string.Empty;

        public TipoEmail Tipo { get; set; }

        /// <summary>Corpo em HTML; vira nulo depois do envio.</summary>
        public string? Html { get; set; }

        /// <summary>Corpo em texto simples (para leitores sem HTML); vira nulo depois do envio.</summary>
        public string? Texto { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? EnviadoEm { get; set; }

        public int Tentativas { get; set; }

        [StringLength(500)]
        public string? UltimoErro { get; set; }

        public bool Desistiu => EnviadoEm == null && Tentativas >= MaximoTentativas;
    }

    public enum TipoEmail
    {
        BoasVindas,
        ConfirmarEmail,
        RedefinirSenha,
        SenhaAlterada,
        PedidoPago,
        ListaDesejos
    }
}
