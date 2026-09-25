using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>
    /// Etapa 3 (compra): carrinho, pedidos com situação e prazo de pagamento, itens apontando para o
    /// produto (no lugar de PedidoJogos), pagamentos e o vínculo da chave com o item em que foi vendida.
    /// </summary>
    public partial class Compra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "Pedidos",
                newName: "Total");

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "PagarAte",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PagoEm",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Pedidos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Pedidos antigos não tinham situação nem data: são considerados pagos, com a data desta migration
            migrationBuilder.Sql("""
                UPDATE "Pedidos" SET "Status" = 'Pago', "CriadoEm" = now(), "PagoEm" = now(), "Total" = GREATEST("Total", 0);
                """);

            migrationBuilder.AddColumn<int>(
                name: "PedidoItemId",
                table: "Chaves",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VendidaEm",
                table: "Chaves",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarrinhoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    AdicionadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrinhoItens", x => x.Id);
                    table.CheckConstraint("CK_CarrinhoItens_Quantidade", "\"Quantidade\" BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_CarrinhoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarrinhoItens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    Metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CodigoTransacao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.CheckConstraint("CK_Pagamentos_Metodo", "\"Metodo\" IN ('Pix', 'Cartao', 'Boleto')");
                    table.CheckConstraint("CK_Pagamentos_Status", "\"Status\" IN ('Pendente', 'Aprovado', 'Recusado')");
                    table.ForeignKey(
                        name: "FK_Pagamentos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoItens", x => x.Id);
                    table.CheckConstraint("CK_PedidoItens_Quantidade", "\"Quantidade\" > 0");
                    table.CheckConstraint("CK_PedidoItens_Valores", "\"PrecoUnitario\" >= 0 AND \"Subtotal\" >= 0");
                    table.ForeignKey(
                        name: "FK_PedidoItens_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // PedidoJogos -> PedidoItens: cada item passa a apontar para o produto do jogo.
            // Jogo vendido que ficou sem produto ganha um produto inativo na plataforma "Não informada".
            migrationBuilder.Sql("""
                INSERT INTO "Plataformas" ("Nome", "Slug", "Ativa")
                SELECT 'Não informada', 'nao-informada', false
                WHERE EXISTS (SELECT 1 FROM "PedidoJogos" pj
                              WHERE NOT EXISTS (SELECT 1 FROM "Produtos" p WHERE p."JogoId" = pj."JogoId"))
                ON CONFLICT ("Slug") DO NOTHING;

                INSERT INTO "Produtos" ("JogoId", "PlataformaId", "Edicao", "Regiao", "Preco", "Ativo")
                SELECT pj."JogoId", (SELECT "Id" FROM "Plataformas" WHERE "Slug" = 'nao-informada'),
                       'Standard', 'Global', GREATEST(max(pj."ValorUnitario"), 0), false
                FROM "PedidoJogos" pj
                WHERE NOT EXISTS (SELECT 1 FROM "Produtos" p WHERE p."JogoId" = pj."JogoId")
                GROUP BY pj."JogoId";

                INSERT INTO "PedidoItens" ("PedidoId", "ProdutoId", "Quantidade", "PrecoUnitario", "Subtotal")
                SELECT pj."PedidoId",
                       (SELECT min(p."Id") FROM "Produtos" p WHERE p."JogoId" = pj."JogoId"),
                       GREATEST(pj."Quantidade", 1), GREATEST(pj."ValorUnitario", 0), GREATEST(pj."ValorTotal", 0)
                FROM "PedidoJogos" pj;

                -- Na etapa 2 nenhuma chave podia estar reservada ou vendida sem pedido
                UPDATE "Chaves" SET "Status" = 'Inativa' WHERE "Status" IN ('Reservada', 'Vendida');
                """);

            migrationBuilder.DropTable(
                name: "PedidoJogos");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Status_PagarAte",
                table: "Pedidos",
                columns: new[] { "Status", "PagarAte" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedidos_Status",
                table: "Pedidos",
                sql: "\"Status\" IN ('AguardandoPagamento', 'Pago', 'Cancelado', 'Reembolsado')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedidos_Total",
                table: "Pedidos",
                sql: "\"Total\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Chaves_PedidoItemId",
                table: "Chaves",
                column: "PedidoItemId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chaves_Pedido",
                table: "Chaves",
                sql: "(\"Status\" IN ('Reservada', 'Vendida') AND \"PedidoItemId\" IS NOT NULL) OR (\"Status\" = 'Disponivel' AND \"PedidoItemId\" IS NULL) OR \"Status\" = 'Inativa'");

            migrationBuilder.CreateIndex(
                name: "IX_CarrinhoItens_ProdutoId",
                table: "CarrinhoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrinhoItens_UsuarioId_ProdutoId",
                table: "CarrinhoItens",
                columns: new[] { "UsuarioId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PedidoId",
                table: "Pagamentos",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_PedidoId_ProdutoId",
                table: "PedidoItens",
                columns: new[] { "PedidoId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_ProdutoId",
                table: "PedidoItens",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chaves_PedidoItens_PedidoItemId",
                table: "Chaves",
                column: "PedidoItemId",
                principalTable: "PedidoItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidoJogos",
                columns: table => new
                {
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    JogoId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_PedidoJogos_JogoId",
                table: "PedidoJogos",
                column: "JogoId");
            // Volta os itens para o formato (pedido, jogo); chaves reservadas voltam ao estoque
            migrationBuilder.Sql("""
                INSERT INTO "PedidoJogos" ("PedidoId", "JogoId", "Quantidade", "ValorUnitario", "ValorTotal")
                SELECT i."PedidoId", p."JogoId", sum(i."Quantidade"), max(i."PrecoUnitario"), sum(i."Subtotal")
                FROM "PedidoItens" i JOIN "Produtos" p ON p."Id" = i."ProdutoId"
                GROUP BY i."PedidoId", p."JogoId";

                UPDATE "Chaves" SET "Status" = 'Disponivel' WHERE "Status" = 'Reservada';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Chaves_PedidoItens_PedidoItemId",
                table: "Chaves");

            migrationBuilder.DropTable(
                name: "CarrinhoItens");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "PedidoItens");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_Status_PagarAte",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedidos_Status",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedidos_Total",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Chaves_PedidoItemId",
                table: "Chaves");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chaves_Pedido",
                table: "Chaves");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PagarAte",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PagoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PedidoItemId",
                table: "Chaves");

            migrationBuilder.DropColumn(
                name: "VendidaEm",
                table: "Chaves");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Pedidos",
                newName: "Valor");

        }
    }
}
