using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Padroniza os e-mails já cadastrados (minúsculas, sem espaços) antes de criar o índice único
            migrationBuilder.Sql("UPDATE \"Usuarios\" SET \"Email\" = LOWER(TRIM(\"Email\"));");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");
        }
    }
}
