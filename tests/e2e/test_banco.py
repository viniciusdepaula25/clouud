"""Banco de dados: nomes em snake_case, regras (check constraints) e conversão de um banco do formato antigo."""
import os
import subprocess

import psycopg
import pytest
from psycopg import errors

from conftest import BANCO, RAIZ, conectar, encontrar_dll, recriar_banco, string_conexao


def test_nomes_em_minusculo(banco):
    """Tabelas, colunas, índices, restrições e sequências sem maiúsculas (só a tabela interna do EF fica como está)."""
    nomes = banco.linhas("""
        SELECT 'tabela', table_name FROM information_schema.tables WHERE table_schema = 'public'
        UNION ALL SELECT 'coluna', table_name || '.' || column_name FROM information_schema.columns WHERE table_schema = 'public'
        UNION ALL SELECT 'índice', indexname FROM pg_indexes WHERE schemaname = 'public'
        UNION ALL SELECT 'restrição', conname FROM pg_constraint WHERE connamespace = 'public'::regnamespace
        UNION ALL SELECT 'sequência', sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public'""")
    com_maiuscula = [(tipo, nome) for tipo, nome in nomes
                     if nome != nome.lower() and not nome.startswith(("__EFMigrationsHistory", "PK___EFMigrationsHistory"))]
    assert com_maiuscula == []
    assert len([n for t, n in nomes if t == "tabela"]) >= 16


@pytest.mark.parametrize("sql, restricao", [
    ("INSERT INTO avaliacoes (usuario_id, jogo_id, nota, criada_em) VALUES ({cliente}, {jogo}, 6, now())", "ck_avaliacoes_nota"),
    ("INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em) VALUES ({cliente}, {produto}, 11, now())",
     "ck_carrinho_itens_quantidade"),
    ("INSERT INTO cupons (codigo, tipo, valor, ativo, criado_em) VALUES ('minusculo', 'Percentual', 10, true, now())", "ck_cupons_codigo"),
    ("INSERT INTO cupons (codigo, tipo, valor, ativo, criado_em) VALUES ('DEMAIS', 'Percentual', 101, true, now())", "ck_cupons_valor"),
    ("INSERT INTO cupons (codigo, tipo, valor, ativo, criado_em) VALUES ('TIPO', 'Brinde', 5, true, now())", "ck_cupons_tipo"),
    ("UPDATE produtos SET preco_promocional = preco WHERE id = {produto}", "ck_produtos_preco_promocional"),
    ("UPDATE produtos SET preco = -1 WHERE id = {produto}", "ck_produtos_preco"),
    ("UPDATE chaves SET status = 'Vendida' WHERE produto_id = {produto}", "ck_chaves_pedido"),  # vendida sem pedido
    ("UPDATE chaves SET status = 'Perdida', pedido_item_id = NULL WHERE produto_id = {produto}",
     "ck_chaves_status|ck_chaves_pedido"),  # status desconhecido quebra as duas regras
    ("UPDATE pedidos SET total = total + 1 WHERE id = {pedido}", "ck_pedidos_desconto"),
    ("UPDATE pedidos SET status = 'Perdido' WHERE id = {pedido}", "ck_pedidos_status"),
])
def test_regras_do_banco_recusam_dados_invalidos(banco, fabrica, sql, restricao):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pedido = fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    comando = sql.format(cliente=cliente["id"], jogo=jogo["jogo_id"], produto=jogo["produto_id"], pedido=pedido)
    with pytest.raises(errors.CheckViolation) as erro:
        banco.executar(comando)
    assert erro.value.diag.constraint_name in restricao.split("|")


def test_email_unico_sem_diferenciar_maiusculas(banco, fabrica):
    cliente = fabrica.cliente()
    with pytest.raises(errors.UniqueViolation):
        banco.executar("INSERT INTO usuarios (name, email, senha, perfil) VALUES ('X', %s, 'x', 0)", cliente["email"])


# ---------------------------------------------------------------- banco antigo

LEGADO = f"{BANCO}_legado"


def dotnet_ef(*argumentos, banco):
    dll = encontrar_dll()
    configuracao = dll.parent.parent.name  # bin/<Release|Debug>/net10.0
    ambiente = os.environ.copy()
    ambiente["ConnectionStrings__LojaJogos"] = string_conexao(banco)
    return subprocess.run(["dotnet", "ef", "database", "update", *argumentos, "--project", "src/Clouud.Web",
                           "--no-build", "--configuration", configuracao],
                          cwd=RAIZ, env=ambiente, capture_output=True, text=True, timeout=600)


@pytest.mark.lento
def test_banco_do_formato_antigo_e_convertido(app):
    """Cria o banco como era no projeto original, põe dados e aplica as migrations novas por cima."""
    versao = subprocess.run(["dotnet", "ef", "--version"], cwd=RAIZ, capture_output=True, text=True)
    if versao.returncode != 0:
        pytest.skip("dotnet-ef indisponível (rode: dotnet tool restore)")
    recriar_banco(LEGADO)
    try:
        r = dotnet_ef("AlinhaModeloAoDiagrama", banco=LEGADO)
        assert r.returncode == 0, r.stdout + r.stderr
        with conectar(LEGADO) as c:
            c.execute("""
                INSERT INTO "Usuarios" ("Name", "Email", "Perfil", "Senha") VALUES ('Ana', 'ana@x.com', 0, '123');
                INSERT INTO "Jogos" ("Nome", "Desenvolvedora", "Plataforma", "Categoria", "Valor", "Foto") VALUES
                  ('Tekken 8', 'Bandai Namco', 'Steam', 'Luta, Ação', 249.90, 'tekken.jpg'),
                  ('Diablo IV', 'Blizzard', 'battle.net', 'RPG/Ação', 199.90, NULL),
                  ('Vendido Sem Loja', 'Y', '', '', 50, NULL);
                INSERT INTO "Pedidos" ("UsuarioId", "Valor") VALUES (1, 299.90);
                INSERT INTO "PedidoJogos" VALUES (1, 1, 1, 249.90, 249.90), (1, 3, 1, 50, 50);""")

        r = dotnet_ef(banco=LEGADO)
        assert r.returncode == 0, r.stdout + r.stderr

        with conectar(LEGADO) as c:
            def linhas(sql):
                return c.execute(sql).fetchall()

            assert linhas("SELECT titulo, slug, capa FROM jogos ORDER BY id") == [
                ("Tekken 8", "tekken-8", "tekken.jpg"), ("Diablo IV", "diablo-iv", None),
                ("Vendido Sem Loja", "vendido-sem-loja", None)]
            assert linhas("""SELECT j.titulo, pl.slug, p.preco::text, p.ativo FROM produtos p
                             JOIN jogos j ON j.id = p.jogo_id JOIN plataformas pl ON pl.id = p.plataforma_id ORDER BY p.id""") == [
                ("Tekken 8", "steam", "249.90", True), ("Diablo IV", "battle-net", "199.90", True),
                ("Vendido Sem Loja", "nao-informada", "50.00", False)]  # vendido sem plataforma: produto inativo
            assert linhas("""SELECT j.titulo, string_agg(c.slug, ',' ORDER BY c.slug) FROM jogo_categorias jc
                             JOIN jogos j ON j.id = jc.jogo_id JOIN categorias c ON c.id = jc.categoria_id
                             GROUP BY j.titulo ORDER BY 1""") == [("Diablo IV", "acao,rpg"), ("Tekken 8", "acao,luta")]
            assert linhas("""SELECT j.titulo, e.nome FROM jogos j JOIN empresas e ON e.id = j.desenvolvedora_id
                             WHERE j.id = 1""") == [("Tekken 8", "Bandai Namco")]
            assert linhas("SELECT status, subtotal::text, desconto::text, total::text, pago_em IS NOT NULL FROM pedidos") == [
                ("Pago", "299.90", "0.00", "299.90", True)]
            assert linhas("SELECT produto_id, quantidade, subtotal::text FROM pedido_itens ORDER BY id") == [
                (1, 1, "249.90"), (3, 1, "50.00")]
            assert linhas("SELECT name, email FROM usuarios") == [("Ana", "ana@x.com")]
    finally:
        try:
            with conectar("postgres") as c:
                c.execute(f'DROP DATABASE IF EXISTS "{LEGADO}" WITH (FORCE)')
        except psycopg.Error:
            pass
