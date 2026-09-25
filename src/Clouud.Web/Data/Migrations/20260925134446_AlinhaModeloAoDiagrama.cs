using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlinhaModeloAoDiagrama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. O nome do cliente passa a ficar só em Usuarios (antes era repetido em Clientes)
            migrationBuilder.Sql("""
                UPDATE "Usuarios" u SET "Name" = c."Nome"
                FROM "Clientes" c
                WHERE c."Id" = u."ID" AND c."Nome" <> '';
                """);

            // 2. Nova tabela de itens do pedido (Pedido_Jogo do diagrama)
            migrationBuilder.CreateTable(
                name: "PedidoJogos",
                columns: table => new
                {
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    JogoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoJogos", x => new { x.PedidoId, x.JogoId });
                    table.ForeignKey(
                        name: "FK_PedidoJogos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoJogos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Copia os itens antigos (se houver), juntando linhas repetidas do mesmo jogo no pedido
            migrationBuilder.Sql("""
                INSERT INTO "PedidoJogos" ("PedidoId", "JogoId", "Quantidade", "ValorUnitario", "ValorTotal")
                SELECT "IdPedido", "IdJogo", SUM("Quantidade"), MAX("ValorUni"), SUM("ValorTotal")
                FROM "PedidoJs"
                GROUP BY "IdPedido", "IdJogo";
                """);

            // 3. Remove as tabelas/colunas que saíram do modelo
            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_PedidoJs_PedidoJId",
                table: "Jogos");

            migrationBuilder.DropTable(
                name: "PedidoJs");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Jogos_PedidoJId",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "PedidoJId",
                table: "Jogos");

            // 4. Preço do jogo: Valorj (double) -> Valor (numeric(10,2)), mantendo os valores
            migrationBuilder.RenameColumn(
                name: "Valorj",
                table: "Jogos",
                newName: "Valor");

            migrationBuilder.AlterColumn<decimal>(
                name: "Valor",
                table: "Jogos",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            // 5. Pedido: coluna do usuário com chave estrangeira de verdade e valor em numeric
            migrationBuilder.RenameColumn(
                name: "Id_Usuario",
                table: "Pedidos",
                newName: "UsuarioId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Valor",
                table: "Pedidos",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            // Pedidos de usuários que não existem não poderiam ser ligados a ninguém
            migrationBuilder.Sql("""
                DELETE FROM "PedidoJogos" WHERE "PedidoId" IN
                    (SELECT "Id" FROM "Pedidos" WHERE "UsuarioId" NOT IN (SELECT "ID" FROM "Usuarios"));
                DELETE FROM "Pedidos" WHERE "UsuarioId" NOT IN (SELECT "ID" FROM "Usuarios");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJogos_JogoId",
                table: "PedidoJogos",
                column: "JogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "PedidoJogos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos");

            migrationBuilder.AlterColumn<double>(
                name: "Valor",
                table: "Jogos",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "Jogos",
                newName: "Valorj");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "Pedidos",
                newName: "Id_Usuario");

            migrationBuilder.AlterColumn<double>(
                name: "Valor",
                table: "Pedidos",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AddColumn<int>(
                name: "PedidoJId",
                table: "Jogos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_Usuarios_Id",
                        column: x => x.Id,
                        principalTable: "Usuarios",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoJs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdJogo = table.Column<int>(type: "integer", nullable: false),
                    IdPedido = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorTotal = table.Column<double>(type: "double precision", nullable: false),
                    ValorUni = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoJs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoJs_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoJs_Jogos_IdJogo",
                        column: x => x.IdJogo,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoJs_Pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoJs_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_PedidoJId",
                table: "Jogos",
                column: "PedidoJId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJs_ClienteId",
                table: "PedidoJs",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJs_IdJogo",
                table: "PedidoJs",
                column: "IdJogo");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJs_IdPedido",
                table: "PedidoJs",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJs_IdUsuario",
                table: "PedidoJs",
                column: "IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Jogos_PedidoJs_PedidoJId",
                table: "Jogos",
                column: "PedidoJId",
                principalTable: "PedidoJs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
