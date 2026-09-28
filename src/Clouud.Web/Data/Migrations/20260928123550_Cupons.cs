using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>Cupons de desconto: tabela cupons e, nos pedidos, subtotal, desconto e o cupom usado.</summary>
    public partial class Cupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cupom_id",
                table: "pedidos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "desconto",
                table: "pedidos",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                table: "pedidos",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "cupons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    pedido_minimo = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    valido_de = table.Column<DateOnly>(type: "date", nullable: true),
                    valido_ate = table.Column<DateOnly>(type: "date", nullable: true),
                    limite_usos = table.Column<int>(type: "integer", nullable: true),
                    limite_por_cliente = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cupons", x => x.id);
                    table.CheckConstraint("ck_cupons_codigo", "codigo = upper(codigo)");
                    table.CheckConstraint("ck_cupons_tipo", "tipo IN ('Percentual', 'ValorFixo')");
                    table.CheckConstraint("ck_cupons_valor", "valor > 0 AND (tipo <> 'Percentual' OR valor <= 100)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_cupom_id",
                table: "pedidos",
                column: "cupom_id");

            // Pedidos antigos não tinham desconto: subtotal = total
            migrationBuilder.Sql("UPDATE pedidos SET subtotal = total, desconto = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pedidos_desconto",
                table: "pedidos",
                sql: "desconto >= 0 AND desconto <= subtotal AND total = subtotal - desconto");

            migrationBuilder.CreateIndex(
                name: "ix_cupons_codigo",
                table: "cupons",
                column: "codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_cupons_cupom_id",
                table: "pedidos",
                column: "cupom_id",
                principalTable: "cupons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_cupons_cupom_id",
                table: "pedidos");

            migrationBuilder.DropTable(
                name: "cupons");

            migrationBuilder.DropIndex(
                name: "ix_pedidos_cupom_id",
                table: "pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pedidos_desconto",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "cupom_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "desconto",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "subtotal",
                table: "pedidos");
        }
    }
}
