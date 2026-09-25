using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>Etapa 4: lista de desejos (usuário + jogo, sem repetir).</summary>
    public partial class ListaDesejos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lista_desejos",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    jogo_id = table.Column<int>(type: "integer", nullable: false),
                    adicionado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lista_desejos", x => new { x.usuario_id, x.jogo_id });
                    table.ForeignKey(
                        name: "fk_lista_desejos_jogos_jogo_id",
                        column: x => x.jogo_id,
                        principalTable: "jogos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lista_desejos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lista_desejos_jogo_id",
                table: "lista_desejos",
                column: "jogo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lista_desejos");
        }
    }
}
