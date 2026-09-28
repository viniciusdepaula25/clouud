using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvisosListaDesejos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "receber_avisos",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true); // contas que já existem (e inserts feitos direto no banco) recebem os avisos

            migrationBuilder.AddColumn<DateTime>(
                name: "aviso_conferido_em",
                table: "lista_desejos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "aviso_disponivel",
                table: "lista_desejos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "aviso_preco_promocao",
                table: "lista_desejos",
                type: "numeric(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "receber_avisos",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "aviso_conferido_em",
                table: "lista_desejos");

            migrationBuilder.DropColumn(
                name: "aviso_disponivel",
                table: "lista_desejos");

            migrationBuilder.DropColumn(
                name: "aviso_preco_promocao",
                table: "lista_desejos");
        }
    }
}
