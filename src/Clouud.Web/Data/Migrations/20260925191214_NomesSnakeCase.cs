using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clouud.Web.Data.Migrations
{
    /// <summary>
    /// Renomeia tabelas, colunas, chaves, índices e check constraints para minúsculo snake_case
    /// ("PedidoItens"."PrecoUnitario" -> pedido_itens.preco_unitario). Só renomeia: nenhum dado é alterado.
    /// </summary>
    public partial class NomesSnakeCase : Migration
    {
        /// <summary>
        /// Renomeia cada sequência de coluna identity para tabela_coluna_seq, com os nomes atuais.
        /// Serve para os dois sentidos: no Down, depois de voltar os nomes, as sequências voltam também.
        /// </summary>
        private const string RenomearSequencias = """
            DO $$
            DECLARE r record;
            BEGIN
                FOR r IN
                    SELECT s.relname AS sequencia, c.relname || '_' || a.attname || '_seq' AS novo_nome
                    FROM pg_class s
                    JOIN pg_depend d ON d.objid = s.oid AND d.deptype = 'i'
                    JOIN pg_class c ON c.oid = d.refobjid
                    JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.refobjsubid
                    WHERE s.relkind = 'S' AND s.relnamespace = current_schema()::regnamespace
                LOOP
                    IF r.sequencia <> r.novo_nome THEN
                        EXECUTE format('ALTER SEQUENCE %I RENAME TO %I', r.sequencia, r.novo_nome);
                    END IF;
                END LOOP;
            END $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarrinhoItens_Produtos_ProdutoId",
                table: "CarrinhoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_CarrinhoItens_Usuarios_UsuarioId",
                table: "CarrinhoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Chaves_PedidoItens_PedidoItemId",
                table: "Chaves");

            migrationBuilder.DropForeignKey(
                name: "FK_Chaves_Produtos_ProdutoId",
                table: "Chaves");

            migrationBuilder.DropForeignKey(
                name: "FK_JogoCategorias_Categorias_CategoriaId",
                table: "JogoCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_JogoCategorias_Jogos_JogoId",
                table: "JogoCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_Empresas_DesenvolvedoraId",
                table: "Jogos");

            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_Empresas_PublicadoraId",
                table: "Jogos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagamentos_Pedidos_PedidoId",
                table: "Pagamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoItens_Pedidos_PedidoId",
                table: "PedidoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoItens_Produtos_ProdutoId",
                table: "PedidoItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Jogos_JogoId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Plataformas_PlataformaId",
                table: "Produtos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Produtos_Preco",
                table: "Produtos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Produtos_PrecoPromocional",
                table: "Produtos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plataformas",
                table: "Plataformas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedidos_Status",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedidos_Total",
                table: "Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pagamentos",
                table: "Pagamentos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pagamentos_Metodo",
                table: "Pagamentos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pagamentos_Status",
                table: "Pagamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jogos",
                table: "Jogos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Empresas",
                table: "Empresas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chaves",
                table: "Chaves");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chaves_Pedido",
                table: "Chaves");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chaves_Status",
                table: "Chaves");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PedidoItens",
                table: "PedidoItens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PedidoItens_Quantidade",
                table: "PedidoItens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PedidoItens_Valores",
                table: "PedidoItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JogoCategorias",
                table: "JogoCategorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarrinhoItens",
                table: "CarrinhoItens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CarrinhoItens_Quantidade",
                table: "CarrinhoItens");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "usuarios");

            migrationBuilder.RenameTable(
                name: "Produtos",
                newName: "produtos");

            migrationBuilder.RenameTable(
                name: "Plataformas",
                newName: "plataformas");

            migrationBuilder.RenameTable(
                name: "Pedidos",
                newName: "pedidos");

            migrationBuilder.RenameTable(
                name: "Pagamentos",
                newName: "pagamentos");

            migrationBuilder.RenameTable(
                name: "Jogos",
                newName: "jogos");

            migrationBuilder.RenameTable(
                name: "Empresas",
                newName: "empresas");

            migrationBuilder.RenameTable(
                name: "Chaves",
                newName: "chaves");

            migrationBuilder.RenameTable(
                name: "Categorias",
                newName: "categorias");

            migrationBuilder.RenameTable(
                name: "PedidoItens",
                newName: "pedido_itens");

            migrationBuilder.RenameTable(
                name: "JogoCategorias",
                newName: "jogo_categorias");

            migrationBuilder.RenameTable(
                name: "CarrinhoItens",
                newName: "carrinho_itens");

            migrationBuilder.RenameColumn(
                name: "Senha",
                table: "usuarios",
                newName: "senha");

            migrationBuilder.RenameColumn(
                name: "Perfil",
                table: "usuarios",
                newName: "perfil");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "usuarios",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Foto",
                table: "usuarios",
                newName: "foto");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "usuarios",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "usuarios",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_Email",
                table: "usuarios",
                newName: "ix_usuarios_email");

            migrationBuilder.RenameColumn(
                name: "Regiao",
                table: "produtos",
                newName: "regiao");

            migrationBuilder.RenameColumn(
                name: "Preco",
                table: "produtos",
                newName: "preco");

            migrationBuilder.RenameColumn(
                name: "Edicao",
                table: "produtos",
                newName: "edicao");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "produtos",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "produtos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PromocaoAte",
                table: "produtos",
                newName: "promocao_ate");

            migrationBuilder.RenameColumn(
                name: "PrecoPromocional",
                table: "produtos",
                newName: "preco_promocional");

            migrationBuilder.RenameColumn(
                name: "PlataformaId",
                table: "produtos",
                newName: "plataforma_id");

            migrationBuilder.RenameColumn(
                name: "JogoId",
                table: "produtos",
                newName: "jogo_id");

            migrationBuilder.RenameIndex(
                name: "IX_Produtos_PlataformaId",
                table: "produtos",
                newName: "ix_produtos_plataforma_id");

            migrationBuilder.RenameIndex(
                name: "IX_Produtos_JogoId_PlataformaId_Edicao",
                table: "produtos",
                newName: "ix_produtos_jogo_id_plataforma_id_edicao");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "plataformas",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "plataformas",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Ativa",
                table: "plataformas",
                newName: "ativa");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "plataformas",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "InstrucoesAtivacao",
                table: "plataformas",
                newName: "instrucoes_ativacao");

            migrationBuilder.RenameIndex(
                name: "IX_Plataformas_Slug",
                table: "plataformas",
                newName: "ix_plataformas_slug");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "pedidos",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "pedidos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "pedidos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "pedidos",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "PagoEm",
                table: "pedidos",
                newName: "pago_em");

            migrationBuilder.RenameColumn(
                name: "PagarAte",
                table: "pedidos",
                newName: "pagar_ate");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "pedidos",
                newName: "criado_em");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "pedidos",
                newName: "ix_pedidos_usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_Status_PagarAte",
                table: "pedidos",
                newName: "ix_pedidos_status_pagar_ate");

            migrationBuilder.RenameColumn(
                name: "Valor",
                table: "pagamentos",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "pagamentos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Metodo",
                table: "pagamentos",
                newName: "metodo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "pagamentos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PedidoId",
                table: "pagamentos",
                newName: "pedido_id");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "pagamentos",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "ConfirmadoEm",
                table: "pagamentos",
                newName: "confirmado_em");

            migrationBuilder.RenameColumn(
                name: "CodigoTransacao",
                table: "pagamentos",
                newName: "codigo_transacao");

            migrationBuilder.RenameIndex(
                name: "IX_Pagamentos_PedidoId",
                table: "pagamentos",
                newName: "ix_pagamentos_pedido_id");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "jogos",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "jogos",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Destaque",
                table: "jogos",
                newName: "destaque");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "jogos",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "Capa",
                table: "jogos",
                newName: "capa");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "jogos",
                newName: "ativo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "jogos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PublicadoraId",
                table: "jogos",
                newName: "publicadora_id");

            migrationBuilder.RenameColumn(
                name: "DesenvolvedoraId",
                table: "jogos",
                newName: "desenvolvedora_id");

            migrationBuilder.RenameColumn(
                name: "DataLancamento",
                table: "jogos",
                newName: "data_lancamento");

            migrationBuilder.RenameColumn(
                name: "ClassificacaoIndicativa",
                table: "jogos",
                newName: "classificacao_indicativa");

            migrationBuilder.RenameIndex(
                name: "IX_Jogos_Slug",
                table: "jogos",
                newName: "ix_jogos_slug");

            migrationBuilder.RenameIndex(
                name: "IX_Jogos_PublicadoraId",
                table: "jogos",
                newName: "ix_jogos_publicadora_id");

            migrationBuilder.RenameIndex(
                name: "IX_Jogos_DesenvolvedoraId",
                table: "jogos",
                newName: "ix_jogos_desenvolvedora_id");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "empresas",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "empresas",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "empresas",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Empresas_Slug",
                table: "empresas",
                newName: "ix_empresas_slug");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "chaves",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Codigo",
                table: "chaves",
                newName: "codigo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "chaves",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendidaEm",
                table: "chaves",
                newName: "vendida_em");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "chaves",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "PedidoItemId",
                table: "chaves",
                newName: "pedido_item_id");

            migrationBuilder.RenameColumn(
                name: "AdicionadaEm",
                table: "chaves",
                newName: "adicionada_em");

            migrationBuilder.RenameIndex(
                name: "IX_Chaves_Codigo",
                table: "chaves",
                newName: "ix_chaves_codigo");

            migrationBuilder.RenameIndex(
                name: "IX_Chaves_ProdutoId_Status",
                table: "chaves",
                newName: "ix_chaves_produto_id_status");

            migrationBuilder.RenameIndex(
                name: "IX_Chaves_PedidoItemId",
                table: "chaves",
                newName: "ix_chaves_pedido_item_id");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "categorias",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "categorias",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "categorias",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Categorias_Slug",
                table: "categorias",
                newName: "ix_categorias_slug");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                table: "pedido_itens",
                newName: "subtotal");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "pedido_itens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "pedido_itens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "pedido_itens",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "PrecoUnitario",
                table: "pedido_itens",
                newName: "preco_unitario");

            migrationBuilder.RenameColumn(
                name: "PedidoId",
                table: "pedido_itens",
                newName: "pedido_id");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoItens_ProdutoId",
                table: "pedido_itens",
                newName: "ix_pedido_itens_produto_id");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoItens_PedidoId_ProdutoId",
                table: "pedido_itens",
                newName: "ix_pedido_itens_pedido_id_produto_id");

            migrationBuilder.RenameColumn(
                name: "CategoriaId",
                table: "jogo_categorias",
                newName: "categoria_id");

            migrationBuilder.RenameColumn(
                name: "JogoId",
                table: "jogo_categorias",
                newName: "jogo_id");

            migrationBuilder.RenameIndex(
                name: "IX_JogoCategorias_CategoriaId",
                table: "jogo_categorias",
                newName: "ix_jogo_categorias_categoria_id");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "carrinho_itens",
                newName: "quantidade");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "carrinho_itens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "carrinho_itens",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "carrinho_itens",
                newName: "produto_id");

            migrationBuilder.RenameColumn(
                name: "AdicionadoEm",
                table: "carrinho_itens",
                newName: "adicionado_em");

            migrationBuilder.RenameIndex(
                name: "IX_CarrinhoItens_UsuarioId_ProdutoId",
                table: "carrinho_itens",
                newName: "ix_carrinho_itens_usuario_id_produto_id");

            migrationBuilder.RenameIndex(
                name: "IX_CarrinhoItens_ProdutoId",
                table: "carrinho_itens",
                newName: "ix_carrinho_itens_produto_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_usuarios",
                table: "usuarios",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_produtos",
                table: "produtos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_plataformas",
                table: "plataformas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pedidos",
                table: "pedidos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pagamentos",
                table: "pagamentos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_jogos",
                table: "jogos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_empresas",
                table: "empresas",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_chaves",
                table: "chaves",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_categorias",
                table: "categorias",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_pedido_itens",
                table: "pedido_itens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_jogo_categorias",
                table: "jogo_categorias",
                columns: new[] { "jogo_id", "categoria_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_carrinho_itens",
                table: "carrinho_itens",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_produtos_preco",
                table: "produtos",
                sql: "preco >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_produtos_preco_promocional",
                table: "produtos",
                sql: "preco_promocional IS NULL OR (preco_promocional >= 0 AND preco_promocional < preco)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pedidos_status",
                table: "pedidos",
                sql: "status IN ('AguardandoPagamento', 'Pago', 'Cancelado', 'Reembolsado')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pedidos_total",
                table: "pedidos",
                sql: "total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pagamentos_metodo",
                table: "pagamentos",
                sql: "metodo IN ('Pix', 'Cartao', 'Boleto')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pagamentos_status",
                table: "pagamentos",
                sql: "status IN ('Pendente', 'Aprovado', 'Recusado')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chaves_pedido",
                table: "chaves",
                sql: "(status IN ('Reservada', 'Vendida') AND pedido_item_id IS NOT NULL) OR (status = 'Disponivel' AND pedido_item_id IS NULL) OR status = 'Inativa'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_chaves_status",
                table: "chaves",
                sql: "status IN ('Disponivel', 'Reservada', 'Vendida', 'Inativa')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pedido_itens_quantidade",
                table: "pedido_itens",
                sql: "quantidade > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pedido_itens_valores",
                table: "pedido_itens",
                sql: "preco_unitario >= 0 AND subtotal >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_carrinho_itens_quantidade",
                table: "carrinho_itens",
                sql: "quantidade BETWEEN 1 AND 10");

            migrationBuilder.AddForeignKey(
                name: "fk_carrinho_itens_produtos_produto_id",
                table: "carrinho_itens",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_carrinho_itens_usuarios_usuario_id",
                table: "carrinho_itens",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_chaves_pedido_itens_pedido_item_id",
                table: "chaves",
                column: "pedido_item_id",
                principalTable: "pedido_itens",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chaves_produtos_produto_id",
                table: "chaves",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_jogo_categorias_categorias_categoria_id",
                table: "jogo_categorias",
                column: "categoria_id",
                principalTable: "categorias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_jogo_categorias_jogos_jogo_id",
                table: "jogo_categorias",
                column: "jogo_id",
                principalTable: "jogos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_jogos_empresas_desenvolvedora_id",
                table: "jogos",
                column: "desenvolvedora_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_jogos_empresas_publicadora_id",
                table: "jogos",
                column: "publicadora_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_pagamentos_pedidos_pedido_id",
                table: "pagamentos",
                column: "pedido_id",
                principalTable: "pedidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_itens_pedidos_pedido_id",
                table: "pedido_itens",
                column: "pedido_id",
                principalTable: "pedidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_itens_produtos_produto_id",
                table: "pedido_itens",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_usuarios_usuario_id",
                table: "pedidos",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_produtos_jogos_jogo_id",
                table: "produtos",
                column: "jogo_id",
                principalTable: "jogos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_produtos_plataformas_plataforma_id",
                table: "produtos",
                column: "plataforma_id",
                principalTable: "plataformas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Sequências das colunas de id seguem o nome da tabela e da coluna (ex.: usuarios_id_seq)
            migrationBuilder.Sql(RenomearSequencias);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_carrinho_itens_produtos_produto_id",
                table: "carrinho_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_carrinho_itens_usuarios_usuario_id",
                table: "carrinho_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_chaves_pedido_itens_pedido_item_id",
                table: "chaves");

            migrationBuilder.DropForeignKey(
                name: "fk_chaves_produtos_produto_id",
                table: "chaves");

            migrationBuilder.DropForeignKey(
                name: "fk_jogo_categorias_categorias_categoria_id",
                table: "jogo_categorias");

            migrationBuilder.DropForeignKey(
                name: "fk_jogo_categorias_jogos_jogo_id",
                table: "jogo_categorias");

            migrationBuilder.DropForeignKey(
                name: "fk_jogos_empresas_desenvolvedora_id",
                table: "jogos");

            migrationBuilder.DropForeignKey(
                name: "fk_jogos_empresas_publicadora_id",
                table: "jogos");

            migrationBuilder.DropForeignKey(
                name: "fk_pagamentos_pedidos_pedido_id",
                table: "pagamentos");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_itens_pedidos_pedido_id",
                table: "pedido_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_itens_produtos_produto_id",
                table: "pedido_itens");

            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_usuarios_usuario_id",
                table: "pedidos");

            migrationBuilder.DropForeignKey(
                name: "fk_produtos_jogos_jogo_id",
                table: "produtos");

            migrationBuilder.DropForeignKey(
                name: "fk_produtos_plataformas_plataforma_id",
                table: "produtos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_usuarios",
                table: "usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "pk_produtos",
                table: "produtos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_produtos_preco",
                table: "produtos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_produtos_preco_promocional",
                table: "produtos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_plataformas",
                table: "plataformas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pedidos",
                table: "pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pedidos_status",
                table: "pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pedidos_total",
                table: "pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pagamentos",
                table: "pagamentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pagamentos_metodo",
                table: "pagamentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pagamentos_status",
                table: "pagamentos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_jogos",
                table: "jogos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_empresas",
                table: "empresas");

            migrationBuilder.DropPrimaryKey(
                name: "pk_chaves",
                table: "chaves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chaves_pedido",
                table: "chaves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_chaves_status",
                table: "chaves");

            migrationBuilder.DropPrimaryKey(
                name: "pk_categorias",
                table: "categorias");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pedido_itens",
                table: "pedido_itens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pedido_itens_quantidade",
                table: "pedido_itens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pedido_itens_valores",
                table: "pedido_itens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_jogo_categorias",
                table: "jogo_categorias");

            migrationBuilder.DropPrimaryKey(
                name: "pk_carrinho_itens",
                table: "carrinho_itens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_carrinho_itens_quantidade",
                table: "carrinho_itens");

            migrationBuilder.RenameTable(
                name: "usuarios",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "produtos",
                newName: "Produtos");

            migrationBuilder.RenameTable(
                name: "plataformas",
                newName: "Plataformas");

            migrationBuilder.RenameTable(
                name: "pedidos",
                newName: "Pedidos");

            migrationBuilder.RenameTable(
                name: "pagamentos",
                newName: "Pagamentos");

            migrationBuilder.RenameTable(
                name: "jogos",
                newName: "Jogos");

            migrationBuilder.RenameTable(
                name: "empresas",
                newName: "Empresas");

            migrationBuilder.RenameTable(
                name: "chaves",
                newName: "Chaves");

            migrationBuilder.RenameTable(
                name: "categorias",
                newName: "Categorias");

            migrationBuilder.RenameTable(
                name: "pedido_itens",
                newName: "PedidoItens");

            migrationBuilder.RenameTable(
                name: "jogo_categorias",
                newName: "JogoCategorias");

            migrationBuilder.RenameTable(
                name: "carrinho_itens",
                newName: "CarrinhoItens");

            migrationBuilder.RenameColumn(
                name: "senha",
                table: "Usuarios",
                newName: "Senha");

            migrationBuilder.RenameColumn(
                name: "perfil",
                table: "Usuarios",
                newName: "Perfil");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Usuarios",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "foto",
                table: "Usuarios",
                newName: "Foto");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Usuarios",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Usuarios",
                newName: "ID");

            migrationBuilder.RenameIndex(
                name: "ix_usuarios_email",
                table: "Usuarios",
                newName: "IX_Usuarios_Email");

            migrationBuilder.RenameColumn(
                name: "regiao",
                table: "Produtos",
                newName: "Regiao");

            migrationBuilder.RenameColumn(
                name: "preco",
                table: "Produtos",
                newName: "Preco");

            migrationBuilder.RenameColumn(
                name: "edicao",
                table: "Produtos",
                newName: "Edicao");

            migrationBuilder.RenameColumn(
                name: "ativo",
                table: "Produtos",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Produtos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "promocao_ate",
                table: "Produtos",
                newName: "PromocaoAte");

            migrationBuilder.RenameColumn(
                name: "preco_promocional",
                table: "Produtos",
                newName: "PrecoPromocional");

            migrationBuilder.RenameColumn(
                name: "plataforma_id",
                table: "Produtos",
                newName: "PlataformaId");

            migrationBuilder.RenameColumn(
                name: "jogo_id",
                table: "Produtos",
                newName: "JogoId");

            migrationBuilder.RenameIndex(
                name: "ix_produtos_plataforma_id",
                table: "Produtos",
                newName: "IX_Produtos_PlataformaId");

            migrationBuilder.RenameIndex(
                name: "ix_produtos_jogo_id_plataforma_id_edicao",
                table: "Produtos",
                newName: "IX_Produtos_JogoId_PlataformaId_Edicao");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "Plataformas",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Plataformas",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "ativa",
                table: "Plataformas",
                newName: "Ativa");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Plataformas",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "instrucoes_ativacao",
                table: "Plataformas",
                newName: "InstrucoesAtivacao");

            migrationBuilder.RenameIndex(
                name: "ix_plataformas_slug",
                table: "Plataformas",
                newName: "IX_Plataformas_Slug");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "Pedidos",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Pedidos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Pedidos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "Pedidos",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "pago_em",
                table: "Pedidos",
                newName: "PagoEm");

            migrationBuilder.RenameColumn(
                name: "pagar_ate",
                table: "Pedidos",
                newName: "PagarAte");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Pedidos",
                newName: "CriadoEm");

            migrationBuilder.RenameIndex(
                name: "ix_pedidos_usuario_id",
                table: "Pedidos",
                newName: "IX_Pedidos_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "ix_pedidos_status_pagar_ate",
                table: "Pedidos",
                newName: "IX_Pedidos_Status_PagarAte");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "Pagamentos",
                newName: "Valor");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Pagamentos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "metodo",
                table: "Pagamentos",
                newName: "Metodo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Pagamentos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "pedido_id",
                table: "Pagamentos",
                newName: "PedidoId");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "Pagamentos",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "confirmado_em",
                table: "Pagamentos",
                newName: "ConfirmadoEm");

            migrationBuilder.RenameColumn(
                name: "codigo_transacao",
                table: "Pagamentos",
                newName: "CodigoTransacao");

            migrationBuilder.RenameIndex(
                name: "ix_pagamentos_pedido_id",
                table: "Pagamentos",
                newName: "IX_Pagamentos_PedidoId");

            migrationBuilder.RenameColumn(
                name: "titulo",
                table: "Jogos",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "Jogos",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "destaque",
                table: "Jogos",
                newName: "Destaque");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "Jogos",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "capa",
                table: "Jogos",
                newName: "Capa");

            migrationBuilder.RenameColumn(
                name: "ativo",
                table: "Jogos",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Jogos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "publicadora_id",
                table: "Jogos",
                newName: "PublicadoraId");

            migrationBuilder.RenameColumn(
                name: "desenvolvedora_id",
                table: "Jogos",
                newName: "DesenvolvedoraId");

            migrationBuilder.RenameColumn(
                name: "data_lancamento",
                table: "Jogos",
                newName: "DataLancamento");

            migrationBuilder.RenameColumn(
                name: "classificacao_indicativa",
                table: "Jogos",
                newName: "ClassificacaoIndicativa");

            migrationBuilder.RenameIndex(
                name: "ix_jogos_slug",
                table: "Jogos",
                newName: "IX_Jogos_Slug");

            migrationBuilder.RenameIndex(
                name: "ix_jogos_publicadora_id",
                table: "Jogos",
                newName: "IX_Jogos_PublicadoraId");

            migrationBuilder.RenameIndex(
                name: "ix_jogos_desenvolvedora_id",
                table: "Jogos",
                newName: "IX_Jogos_DesenvolvedoraId");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "Empresas",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Empresas",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Empresas",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_empresas_slug",
                table: "Empresas",
                newName: "IX_Empresas_Slug");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Chaves",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "codigo",
                table: "Chaves",
                newName: "Codigo");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Chaves",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vendida_em",
                table: "Chaves",
                newName: "VendidaEm");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "Chaves",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "pedido_item_id",
                table: "Chaves",
                newName: "PedidoItemId");

            migrationBuilder.RenameColumn(
                name: "adicionada_em",
                table: "Chaves",
                newName: "AdicionadaEm");

            migrationBuilder.RenameIndex(
                name: "ix_chaves_codigo",
                table: "Chaves",
                newName: "IX_Chaves_Codigo");

            migrationBuilder.RenameIndex(
                name: "ix_chaves_produto_id_status",
                table: "Chaves",
                newName: "IX_Chaves_ProdutoId_Status");

            migrationBuilder.RenameIndex(
                name: "ix_chaves_pedido_item_id",
                table: "Chaves",
                newName: "IX_Chaves_PedidoItemId");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "Categorias",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Categorias",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Categorias",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_categorias_slug",
                table: "Categorias",
                newName: "IX_Categorias_Slug");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "PedidoItens",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "PedidoItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PedidoItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "PedidoItens",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "preco_unitario",
                table: "PedidoItens",
                newName: "PrecoUnitario");

            migrationBuilder.RenameColumn(
                name: "pedido_id",
                table: "PedidoItens",
                newName: "PedidoId");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_itens_produto_id",
                table: "PedidoItens",
                newName: "IX_PedidoItens_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "ix_pedido_itens_pedido_id_produto_id",
                table: "PedidoItens",
                newName: "IX_PedidoItens_PedidoId_ProdutoId");

            migrationBuilder.RenameColumn(
                name: "categoria_id",
                table: "JogoCategorias",
                newName: "CategoriaId");

            migrationBuilder.RenameColumn(
                name: "jogo_id",
                table: "JogoCategorias",
                newName: "JogoId");

            migrationBuilder.RenameIndex(
                name: "ix_jogo_categorias_categoria_id",
                table: "JogoCategorias",
                newName: "IX_JogoCategorias_CategoriaId");

            migrationBuilder.RenameColumn(
                name: "quantidade",
                table: "CarrinhoItens",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CarrinhoItens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "CarrinhoItens",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "produto_id",
                table: "CarrinhoItens",
                newName: "ProdutoId");

            migrationBuilder.RenameColumn(
                name: "adicionado_em",
                table: "CarrinhoItens",
                newName: "AdicionadoEm");

            migrationBuilder.RenameIndex(
                name: "ix_carrinho_itens_usuario_id_produto_id",
                table: "CarrinhoItens",
                newName: "IX_CarrinhoItens_UsuarioId_ProdutoId");

            migrationBuilder.RenameIndex(
                name: "ix_carrinho_itens_produto_id",
                table: "CarrinhoItens",
                newName: "IX_CarrinhoItens_ProdutoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Produtos",
                table: "Produtos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plataformas",
                table: "Plataformas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pedidos",
                table: "Pedidos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pagamentos",
                table: "Pagamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jogos",
                table: "Jogos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Empresas",
                table: "Empresas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chaves",
                table: "Chaves",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categorias",
                table: "Categorias",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PedidoItens",
                table: "PedidoItens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JogoCategorias",
                table: "JogoCategorias",
                columns: new[] { "JogoId", "CategoriaId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarrinhoItens",
                table: "CarrinhoItens",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Produtos_Preco",
                table: "Produtos",
                sql: "\"Preco\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Produtos_PrecoPromocional",
                table: "Produtos",
                sql: "\"PrecoPromocional\" IS NULL OR (\"PrecoPromocional\" >= 0 AND \"PrecoPromocional\" < \"Preco\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedidos_Status",
                table: "Pedidos",
                sql: "\"Status\" IN ('AguardandoPagamento', 'Pago', 'Cancelado', 'Reembolsado')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedidos_Total",
                table: "Pedidos",
                sql: "\"Total\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pagamentos_Metodo",
                table: "Pagamentos",
                sql: "\"Metodo\" IN ('Pix', 'Cartao', 'Boleto')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pagamentos_Status",
                table: "Pagamentos",
                sql: "\"Status\" IN ('Pendente', 'Aprovado', 'Recusado')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chaves_Pedido",
                table: "Chaves",
                sql: "(\"Status\" IN ('Reservada', 'Vendida') AND \"PedidoItemId\" IS NOT NULL) OR (\"Status\" = 'Disponivel' AND \"PedidoItemId\" IS NULL) OR \"Status\" = 'Inativa'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chaves_Status",
                table: "Chaves",
                sql: "\"Status\" IN ('Disponivel', 'Reservada', 'Vendida', 'Inativa')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PedidoItens_Quantidade",
                table: "PedidoItens",
                sql: "\"Quantidade\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PedidoItens_Valores",
                table: "PedidoItens",
                sql: "\"PrecoUnitario\" >= 0 AND \"Subtotal\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CarrinhoItens_Quantidade",
                table: "CarrinhoItens",
                sql: "\"Quantidade\" BETWEEN 1 AND 10");

            migrationBuilder.AddForeignKey(
                name: "FK_CarrinhoItens_Produtos_ProdutoId",
                table: "CarrinhoItens",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarrinhoItens_Usuarios_UsuarioId",
                table: "CarrinhoItens",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chaves_PedidoItens_PedidoItemId",
                table: "Chaves",
                column: "PedidoItemId",
                principalTable: "PedidoItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chaves_Produtos_ProdutoId",
                table: "Chaves",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JogoCategorias_Categorias_CategoriaId",
                table: "JogoCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JogoCategorias_Jogos_JogoId",
                table: "JogoCategorias",
                column: "JogoId",
                principalTable: "Jogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Pagamentos_Pedidos_PedidoId",
                table: "Pagamentos",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoItens_Pedidos_PedidoId",
                table: "PedidoItens",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoItens_Produtos_ProdutoId",
                table: "PedidoItens",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Jogos_JogoId",
                table: "Produtos",
                column: "JogoId",
                principalTable: "Jogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Plataformas_PlataformaId",
                table: "Produtos",
                column: "PlataformaId",
                principalTable: "Plataformas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Sequências das colunas de id seguem o nome da tabela e da coluna (ex.: usuarios_id_seq)
            migrationBuilder.Sql(RenomearSequencias);
        }

    }
}
