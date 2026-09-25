using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>
    /// Etapa 2 (estoque): cria a tabela Chaves. Cada chave pertence a um produto, tem código único
    /// em toda a loja e a situação gravada como texto (Disponivel, Reservada, Vendida, Inativa).
    /// </summary>
    public partial class Estoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdicionadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chaves", x => x.Id);
                    table.CheckConstraint("CK_Chaves_Status", "\"Status\" IN ('Disponivel', 'Reservada', 'Vendida', 'Inativa')");
                    table.ForeignKey(
                        name: "FK_Chaves_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chaves_Codigo",
                table: "Chaves",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chaves_ProdutoId_Status",
                table: "Chaves",
                columns: new[] { "ProdutoId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chaves");
        }
    }
}
