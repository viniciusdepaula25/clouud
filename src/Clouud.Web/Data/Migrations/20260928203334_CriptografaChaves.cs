using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriptografaChaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Os códigos continuam na mesma coluna (agora codigo_cifrado). Os que estavam em texto puro são
            // cifrados em C# ao iniciar a aplicação (Infraestrutura/ConversaoChavesLegadas.cs), porque a chave
            // de criptografia não pode aparecer no SQL.
            migrationBuilder.DropIndex(
                name: "ix_chaves_codigo",
                table: "chaves");

            migrationBuilder.RenameColumn(
                name: "codigo",
                table: "chaves",
                newName: "codigo_cifrado");

            migrationBuilder.AlterColumn<string>(
                name: "codigo_cifrado",
                table: "chaves",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "codigo_hash",
                table: "chaves",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chaves_codigo_hash",
                table: "chaves",
                column: "codigo_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chaves_codigo_hash",
                table: "chaves");

            migrationBuilder.DropColumn(
                name: "codigo_hash",
                table: "chaves");

            // Atenção: voltar esta migration não decifra os códigos (isso exige a chave de criptografia);
            // a coluna volta a se chamar "codigo", ainda como texto longo, com os valores cifrados.
            migrationBuilder.RenameColumn(
                name: "codigo_cifrado",
                table: "chaves",
                newName: "codigo");

            migrationBuilder.CreateIndex(
                name: "ix_chaves_codigo",
                table: "chaves",
                column: "codigo",
                unique: true);
        }
    }
}
