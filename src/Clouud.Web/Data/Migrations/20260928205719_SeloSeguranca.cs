using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeloSeguranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selo_seguranca",
                table: "usuarios",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                // Um selo aleatório para cada conta que já existe (e para inserts feitos direto no banco).
                // Os cookies de login de antes desta versão não têm selo: quem estava logado entra de novo.
                defaultValueSql: "md5(random()::text || clock_timestamp()::text)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selo_seguranca",
                table: "usuarios");
        }
    }
}
