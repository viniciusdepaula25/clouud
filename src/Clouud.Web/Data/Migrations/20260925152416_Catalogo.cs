using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>
    /// Etapa 1 (catálogo): cria Plataformas, Categorias, Empresas e Produtos e reformula Jogos.
    /// Os textos antigos de Jogos (Plataforma, Categoria, Desenvolvedora) viram linhas nas tabelas
    /// novas, e o Valor de cada jogo vira o preço de um Produto na plataforma dele.
    /// </summary>
    public partial class Catalogo : Migration
    {
        /// <summary>
        /// Expressão SQL equivalente ao SlugHelper.Gerar: "Ação & Aventura" -> "acao-aventura".
        /// </summary>
        private static string Slug(string expressao) =>
            $"""
            trim(both '-' from regexp_replace(
                translate(lower(trim({expressao})), 'áàâãäåéèêëíìîïóòôõöúùûüçñý', 'aaaaaaeeeeiiiiooooouuuucny'),
                '[^a-z0-9]+', '-', 'g'))
            """;

        /// <summary>Slug da plataforma, com os nomes antigos mais comuns apontando para as plataformas já cadastradas.</summary>
        private static string SlugPlataforma(string expressao) =>
            $"""
            CASE {Slug(expressao)}
                WHEN 'epic' THEN 'epic-games'
                WHEN 'epic-games-store' THEN 'epic-games'
                WHEN 'ubisoft' THEN 'ubisoft-connect'
                WHEN 'uplay' THEN 'ubisoft-connect'
                WHEN 'obsofit' THEN 'ubisoft-connect'
                WHEN 'battlenet' THEN 'battle-net'
                WHEN 'blizzard' THEN 'battle-net'
                WHEN 'origin' THEN 'ea-app'
                WHEN 'ea' THEN 'ea-app'
                WHEN 'ps4' THEN 'playstation'
                WHEN 'ps5' THEN 'playstation'
                WHEN 'psn' THEN 'playstation'
                WHEN 'nintendo' THEN 'nintendo-eshop'
                WHEN 'nintendo-switch' THEN 'nintendo-eshop'
                WHEN 'switch' THEN 'nintendo-eshop'
                WHEN 'xbox-one' THEN 'xbox'
                WHEN 'xbox-series' THEN 'xbox'
                ELSE {Slug(expressao)}
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. Tabelas novas ----
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plataformas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    InstrucoesAtivacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plataformas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Slug",
                table: "Categorias",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Slug",
                table: "Empresas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plataformas_Slug",
                table: "Plataformas",
                column: "Slug",
                unique: true);

            // ---- 2. Plataformas e categorias iniciais ----
            migrationBuilder.Sql("""
                INSERT INTO "Plataformas" ("Nome", "Slug", "InstrucoesAtivacao", "Ativa") VALUES
                ('Steam', 'steam', 'Abra o Steam, clique em "Adicionar um jogo" (canto inferior esquerdo) > "Ativar um produto no Steam..." e digite a chave.', true),
                ('Epic Games', 'epic-games', 'Abra a Epic Games Store, clique no seu perfil > "Resgatar código" e digite a chave.', true),
                ('Ubisoft Connect', 'ubisoft-connect', 'Abra o Ubisoft Connect, clique no menu (canto superior esquerdo) > "Ativar chave" e digite a chave.', true),
                ('EA App', 'ea-app', 'Abra o EA App, clique no menu (três linhas) > "Resgatar código" e digite a chave.', true),
                ('Battle.net', 'battle-net', 'Abra o Battle.net, clique no seu perfil > "Resgatar código" e digite a chave.', true),
                ('GOG', 'gog', 'Acesse gog.com/redeem, entre na sua conta e digite a chave.', true),
                ('Xbox', 'xbox', 'Acesse microsoft.com/redeem (ou a Microsoft Store no console) e digite o código de 25 caracteres.', true),
                ('PlayStation', 'playstation', 'No console, abra a PlayStation Store > "..." > "Resgatar código" e digite o código.', true),
                ('Nintendo eShop', 'nintendo-eshop', 'No console, abra o Nintendo eShop > "Resgatar código" e digite o código de 16 caracteres.', true)
                ON CONFLICT ("Slug") DO NOTHING;

                INSERT INTO "Categorias" ("Nome", "Slug") VALUES
                ('Ação', 'acao'), ('Aventura', 'aventura'), ('RPG', 'rpg'), ('Tiro', 'tiro'),
                ('Esportes', 'esportes'), ('Corrida', 'corrida'), ('Luta', 'luta'), ('Estratégia', 'estrategia'),
                ('Simulação', 'simulacao'), ('Terror', 'terror'), ('Indie', 'indie'),
                ('Multijogador', 'multijogador'), ('Mundo aberto', 'mundo-aberto')
                ON CONFLICT ("Slug") DO NOTHING;
                """);

            // ---- 3. Textos antigos de Jogos viram linhas nas tabelas novas ----
            migrationBuilder.Sql($"""
                INSERT INTO "Plataformas" ("Nome", "Slug", "Ativa")
                SELECT DISTINCT ON (slug) nome, slug, true
                FROM (SELECT trim("Plataforma") AS nome, {SlugPlataforma("\"Plataforma\"")} AS slug FROM "Jogos") t
                WHERE slug <> ''
                ORDER BY slug, nome
                ON CONFLICT ("Slug") DO NOTHING;
                """);

            // "Ação, Aventura" ou "Ação/Aventura" viram duas categorias
            migrationBuilder.Sql($"""
                CREATE TEMP TABLE jogo_categoria_antiga AS
                SELECT j."Id" AS jogo_id, trim(parte) AS nome, {Slug("parte")} AS slug
                FROM "Jogos" j, regexp_split_to_table(j."Categoria", '[,/;|]') AS parte;

                INSERT INTO "Categorias" ("Nome", "Slug")
                SELECT DISTINCT ON (slug) nome, slug
                FROM jogo_categoria_antiga
                WHERE slug <> ''
                ORDER BY slug, nome
                ON CONFLICT ("Slug") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "Empresas" ("Nome", "Slug")
                SELECT DISTINCT ON (slug) nome, slug
                FROM (SELECT trim("Desenvolvedora") AS nome, {Slug("\"Desenvolvedora\"")} AS slug FROM "Jogos") t
                WHERE slug <> ''
                ORDER BY slug, nome
                ON CONFLICT ("Slug") DO NOTHING;
                """);

            // ---- 4. Jogos reformulado ----
            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Jogos",
                newName: "Titulo");

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Jogos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.RenameColumn(
                name: "Foto",
                table: "Jogos",
                newName: "Capa");

            migrationBuilder.AlterColumn<string>(
                name: "Capa",
                table: "Jogos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Jogos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Jogos",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataLancamento",
                table: "Jogos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificacaoIndicativa",
                table: "Jogos",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DesenvolvedoraId",
                table: "Jogos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicadoraId",
                table: "Jogos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Destaque",
                table: "Jogos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Jogos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Jogos que já existiam continuam na loja; títulos repetidos ganham o Id no fim do slug
            migrationBuilder.Sql($"""
                UPDATE "Jogos" SET "Ativo" = true;

                WITH base AS (
                    SELECT "Id", COALESCE(NULLIF(left({Slug("\"Titulo\"")}, 140), ''), 'jogo') AS slug
                    FROM "Jogos"
                ), numerado AS (
                    SELECT "Id", slug, row_number() OVER (PARTITION BY slug ORDER BY "Id") AS n FROM base
                )
                UPDATE "Jogos" j
                SET "Slug" = CASE WHEN numerado.n = 1 THEN numerado.slug ELSE numerado.slug || '-' || j."Id" END
                FROM numerado
                WHERE numerado."Id" = j."Id";

                UPDATE "Jogos" j
                SET "DesenvolvedoraId" = e."Id"
                FROM "Empresas" e
                WHERE e."Slug" = {Slug("j.\"Desenvolvedora\"")};
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Slug",
                table: "Jogos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_DesenvolvedoraId",
                table: "Jogos",
                column: "DesenvolvedoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_PublicadoraId",
                table: "Jogos",
                column: "PublicadoraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jogos_Empresas_DesenvolvedoraId",
                table: "Jogos",
                column: "DesenvolvedoraId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Jogos_Empresas_PublicadoraId",
                table: "Jogos",
                column: "PublicadoraId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ---- 5. Categorias dos jogos (N:N) ----
            migrationBuilder.CreateTable(
                name: "JogoCategorias",
                columns: table => new
                {
                    JogoId = table.Column<int>(type: "integer", nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JogoCategorias", x => new { x.JogoId, x.CategoriaId });
                    table.ForeignKey(
                        name: "FK_JogoCategorias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JogoCategorias_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JogoCategorias_CategoriaId",
                table: "JogoCategorias",
                column: "CategoriaId");

            migrationBuilder.Sql("""
                INSERT INTO "JogoCategorias" ("JogoId", "CategoriaId")
                SELECT DISTINCT a.jogo_id, c."Id"
                FROM jogo_categoria_antiga a
                JOIN "Categorias" c ON c."Slug" = a.slug;

                DROP TABLE jogo_categoria_antiga;
                """);

            // ---- 6. Produtos: o Valor de cada jogo vira o preço na plataforma dele ----
            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JogoId = table.Column<int>(type: "integer", nullable: false),
                    PlataformaId = table.Column<int>(type: "integer", nullable: false),
                    Edicao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Regiao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PrecoPromocional = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PromocaoAte = table.Column<DateOnly>(type: "date", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.CheckConstraint("CK_Produtos_Preco", "\"Preco\" >= 0");
                    table.CheckConstraint("CK_Produtos_PrecoPromocional", "\"PrecoPromocional\" IS NULL OR (\"PrecoPromocional\" >= 0 AND \"PrecoPromocional\" < \"Preco\")");
                    table.ForeignKey(
                        name: "FK_Produtos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Produtos_Plataformas_PlataformaId",
                        column: x => x.PlataformaId,
                        principalTable: "Plataformas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_JogoId_PlataformaId_Edicao",
                table: "Produtos",
                columns: new[] { "JogoId", "PlataformaId", "Edicao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_PlataformaId",
                table: "Produtos",
                column: "PlataformaId");

            migrationBuilder.Sql($"""
                INSERT INTO "Produtos" ("JogoId", "PlataformaId", "Edicao", "Regiao", "Preco", "Ativo")
                SELECT j."Id", p."Id", 'Standard', 'Global', GREATEST(j."Valor", 0), true
                FROM "Jogos" j
                JOIN "Plataformas" p ON p."Slug" = {SlugPlataforma("j.\"Plataforma\"")};
                """);

            // ---- 7. Colunas antigas, agora nas tabelas novas ----
            migrationBuilder.DropColumn(
                name: "Plataforma",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Desenvolvedora",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Valor",
                table: "Jogos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volta para os textos dentro de Jogos. Cada jogo fica com a primeira categoria (em ordem
            // alfabética) e com a plataforma e o preço do seu primeiro produto.
            migrationBuilder.AddColumn<string>(
                name: "Plataforma",
                table: "Jogos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Jogos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Desenvolvedora",
                table: "Jogos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Valor",
                table: "Jogos",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Jogos" j
                SET "Plataforma" = left(p."Nome", 50), "Valor" = pr."Preco"
                FROM (SELECT DISTINCT ON ("JogoId") * FROM "Produtos" ORDER BY "JogoId", "Id") pr
                JOIN "Plataformas" p ON p."Id" = pr."PlataformaId"
                WHERE pr."JogoId" = j."Id";

                UPDATE "Jogos" j
                SET "Categoria" = left(c."Nome", 20)
                FROM (SELECT DISTINCT ON (jc."JogoId") jc."JogoId", c."Nome"
                      FROM "JogoCategorias" jc JOIN "Categorias" c ON c."Id" = jc."CategoriaId"
                      ORDER BY jc."JogoId", c."Nome") c
                WHERE c."JogoId" = j."Id";

                UPDATE "Jogos" j
                SET "Desenvolvedora" = left(e."Nome", 50)
                FROM "Empresas" e
                WHERE e."Id" = j."DesenvolvedoraId";

                UPDATE "Jogos" SET "Titulo" = left("Titulo", 50);
                """);

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "JogoCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_Empresas_DesenvolvedoraId",
                table: "Jogos");

            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_Empresas_PublicadoraId",
                table: "Jogos");

            migrationBuilder.DropIndex(
                name: "IX_Jogos_DesenvolvedoraId",
                table: "Jogos");

            migrationBuilder.DropIndex(
                name: "IX_Jogos_PublicadoraId",
                table: "Jogos");

            migrationBuilder.DropIndex(
                name: "IX_Jogos_Slug",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Destaque",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "PublicadoraId",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "DesenvolvedoraId",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "ClassificacaoIndicativa",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "DataLancamento",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Jogos");

            migrationBuilder.AlterColumn<string>(
                name: "Capa",
                table: "Jogos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "Capa",
                table: "Jogos",
                newName: "Foto");

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Jogos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "Jogos",
                newName: "Nome");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Plataformas");
        }
    }
}
