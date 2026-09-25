using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Models
{
    /// <summary>Situação da chave no estoque. Gravada no banco como texto ("Disponivel", "Vendida"...).</summary>
    public enum StatusChave
    {
        /// <summary>No estoque, pode ser vendida.</summary>
        Disponivel,
        /// <summary>Separada para um pedido que ainda não foi pago.</summary>
        Reservada,
        /// <summary>Entregue a um cliente.</summary>
        Vendida,
        /// <summary>Fora de venda: retirada pelo admin ou de um pedido reembolsado (o cliente já viu o código).</summary>
        Inativa
    }

    public static class StatusChaveExtensions
    {
        /// <summary>Nome para mostrar na tela, com acento.</summary>
        public static string Descricao(this StatusChave status) => status switch
        {
            StatusChave.Disponivel => "Disponível",
            StatusChave.Reservada => "Reservada",
            StatusChave.Vendida => "Vendida",
            StatusChave.Inativa => "Inativa",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Uma chave de ativação do estoque de um <see cref="Produto"/>. Cada chave é vendida uma única vez,
    /// por isso o código é único em toda a loja (a mesma chave não entra em dois produtos).
    /// </summary>
    [Table("Chaves")]
    [Index(nameof(Codigo), IsUnique = true)]
    [Index(nameof(ProdutoId), nameof(Status))]
    public class Chave
    {
        public const int TamanhoMaximoCodigo = 200;

        [Key]
        public int Id { get; set; }

        public int ProdutoId { get; set; }

        [ForeignKey(nameof(ProdutoId))]
        public Produto Produto { get; set; } = null!;

        [Required]
        [StringLength(TamanhoMaximoCodigo)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        public StatusChave Status { get; set; } = StatusChave.Disponivel;

        [Display(Name = "Adicionada em")]
        public DateTime AdicionadaEm { get; set; } = DateTime.UtcNow;

        /// <summary>Item do pedido que reservou ou comprou a chave. Nulo enquanto está no estoque.</summary>
        public int? PedidoItemId { get; set; }

        [ForeignKey(nameof(PedidoItemId))]
        public PedidoItem? PedidoItem { get; set; }

        [Display(Name = "Vendida em")]
        public DateTime? VendidaEm { get; set; }

        /// <summary>Pode voltar ao estoque ou ser excluída: nunca foi para um pedido.</summary>
        public bool NuncaFoiVendida => PedidoItemId == null;

        /// <summary>"ABCDE-FGHIJ-KLMNO" -> "ABCDE-*****-*LMNO": mostra só o começo e o fim.</summary>
        public string CodigoMascarado()
        {
            if (Codigo.Length <= 8)
            {
                return new string('*', Codigo.Length);
            }
            var meio = Codigo[4..^4];
            return Codigo[..4] + new string(meio.Select(c => char.IsLetterOrDigit(c) ? '*' : c).ToArray()) + Codigo[^4..];
        }
    }
}
