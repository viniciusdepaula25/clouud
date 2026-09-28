using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>Avaliações dos jogos: nota de 1 a 5 e comentário, uma por cliente e jogo.</summary>
    public partial class Avaliacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avaliacoes",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    jogo_id = table.Column<int>(type: "integer", nullable: false),
                    nota = table.Column<int>(type: "integer", nullable: false),
                    comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avaliacoes", x => new { x.usuario_id, x.jogo_id });
                    table.CheckConstraint("ck_avaliacoes_nota", "nota BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_avaliacoes_jogos_jogo_id",
                        column: x => x.jogo_id,
                        principalTable: "jogos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_avaliacoes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avaliacoes_jogo_id_criada_em",
                table: "avaliacoes",
                columns: new[] { "jogo_id", "criada_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avaliacoes");
        }
    }
}
