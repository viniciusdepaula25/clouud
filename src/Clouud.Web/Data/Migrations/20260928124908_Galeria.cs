using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>Galeria de imagens dos jogos (tabela jogo_imagens).</summary>
    public partial class Galeria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jogo_imagens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    jogo_id = table.Column<int>(type: "integer", nullable: false),
                    arquivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    legenda = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jogo_imagens", x => x.id);
                    table.ForeignKey(
                        name: "fk_jogo_imagens_jogos_jogo_id",
                        column: x => x.jogo_id,
                        principalTable: "jogos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jogo_imagens_jogo_id_ordem",
                table: "jogo_imagens",
                columns: new[] { "jogo_id", "ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jogo_imagens");
        }
    }
}
