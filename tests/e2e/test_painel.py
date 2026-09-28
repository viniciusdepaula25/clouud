"""Painel do admin: números do período conferidos com o banco, gráfico, dica, mais vendidos e reposição."""
from decimal import ROUND_HALF_EVEN, Decimal

import pytest

from conftest import FUSO, brl


def inicio(dias):
    """Início do período no mesmo critério da aplicação: hoje e os dias-1 anteriores, no horário local."""
    return (f"((date_trunc('day', now() AT TIME ZONE '{FUSO}') - interval '{dias - 1} days') AT TIME ZONE '{FUSO}')")


def numeros(banco, dias):
    pagos = f"FROM pedidos WHERE status = 'Pago' AND pago_em >= {inicio(dias)}"
    return {
        "faturamento": banco.valor(f"SELECT coalesce(sum(total), 0) {pagos}"),
        "anterior": banco.valor(f"""SELECT coalesce(sum(total), 0) FROM pedidos WHERE status = 'Pago'
                                     AND pago_em >= {inicio(dias)} - interval '{dias} days' AND pago_em < {inicio(dias)}"""),
        "pedidos": banco.valor(f"SELECT count(*) {pagos}"),
        "chaves": banco.valor(f"""SELECT coalesce(sum(i.quantidade), 0) FROM pedido_itens i JOIN pedidos p ON p.id = i.pedido_id
                                   WHERE p.status = 'Pago' AND p.pago_em >= {inicio(dias)}"""),
    }


def variacao_esperada(atual: Decimal, anterior: Decimal):
    if anterior == 0:
        return None
    return int(((atual - anterior) / anterior * 100).quantize(Decimal(1), rounding=ROUND_HALF_EVEN))


@pytest.fixture(scope="module")
def vendas(fabrica):
    """Vendas em datas conhecidas: hoje, 3 dias atrás, 20 dias atrás, 40 dias atrás (período anterior) e um reembolso."""
    cliente = fabrica.cliente()
    campeao = fabrica.jogo(preco=10, chaves=0)  # esgotado e muito vendido: aparece nas duas listas
    outro = fabrica.jogo(preco=80, chaves=1)
    fabrica.pedido_pago(cliente["id"], campeao["produto_id"], quantidade=50, preco=10)
    for _ in range(3):  # o mais desejado: vai para o topo da lista "repor primeiro" (que mostra só 8)
        fabrica.banco.executar("INSERT INTO lista_desejos (usuario_id, jogo_id, adicionado_em) VALUES (%s, %s, now())",
                               fabrica.cliente()["id"], campeao["jogo_id"])
    fabrica.pedido_pago(cliente["id"], outro["produto_id"], preco=80, pago_ha_dias=3)
    fabrica.pedido_pago(cliente["id"], outro["produto_id"], preco=80, pago_ha_dias=20)
    fabrica.pedido_pago(cliente["id"], outro["produto_id"], preco=80, pago_ha_dias=40)
    fabrica.pedido_pago(cliente["id"], outro["produto_id"], preco=999, status="Reembolsado")
    return campeao, outro


def test_visitante_e_cliente_nao_veem_o_painel(paginas, fabrica):
    assert paginas.nova().request.get("/Admin", max_redirects=0).status == 302
    cliente = fabrica.cliente()
    assert paginas.logada(cliente["email"], cliente["senha"]).request.get("/Admin", max_redirects=0).status == 302


@pytest.mark.parametrize("dias", [7, 30, 90])
def test_numeros_do_periodo_batem_com_o_banco(paginas, banco, vendas, dias):
    admin = paginas.admin()
    admin.goto(f"/Admin?dias={dias}")
    esperado = numeros(banco, dias)
    assert admin.inner_text("#faturamento") == brl(esperado["faturamento"])  # reembolsado fica fora
    assert admin.inner_text("#pedidosPagos") == str(esperado["pedidos"])
    assert admin.inner_text("#chavesVendidas") == str(esperado["chaves"])
    assert admin.locator("a[aria-current=true]").inner_text().strip() == f"Últimos {dias} dias"
    assert admin.locator("#graficoVendas .coluna").count() == dias

    texto = admin.inner_text("#variacaoFaturamento") if admin.locator("#variacaoFaturamento").count() else ""
    v = variacao_esperada(esperado["faturamento"], esperado["anterior"])
    if v is None:
        assert "sem vendas" in texto
    elif v == 0:
        assert "igual" in texto
    else:
        assert f"{abs(v)}%" in texto and ("▲" in texto) == (v > 0), texto


def test_grafico_dica_tabela_e_listas(paginas, banco, vendas):
    campeao, _ = vendas
    admin = paginas.admin()
    admin.goto("/Admin?dias=90")
    colunas = admin.locator("#graficoVendas .coluna")
    dica = admin.locator("#tooltipVendas")
    colunas.last.hover()
    hoje = banco.valor(f"SELECT coalesce(sum(total), 0) FROM pedidos WHERE status = 'Pago' AND pago_em >= {inicio(1)}")
    assert dica.is_visible() and dica.locator("strong").inner_text() == brl(hoje) and "pedido(s)" in dica.inner_text()
    admin.mouse.move(5, 5)
    assert not dica.is_visible()
    colunas.first.focus()
    assert dica.is_visible()  # também pelo teclado
    assert ":" in colunas.first.get_attribute("aria-label")

    admin.click("summary:has-text('Ver os dados em tabela')")
    dias_com_venda = banco.valor(f"""SELECT count(DISTINCT (pago_em AT TIME ZONE '{FUSO}')::date) FROM pedidos
                                      WHERE status = 'Pago' AND pago_em >= {inicio(90)}""")
    assert admin.locator("details table tbody tr").count() == dias_com_venda

    assert campeao["titulo"] in admin.locator("#tabelaMaisVendidos tbody tr").first.inner_text()
    assert "50" in admin.locator("#tabelaMaisVendidos tbody tr").first.inner_text()
    assert campeao["titulo"] in admin.locator("#tabelaRepor tbody tr").first.inner_text()

    assert admin.inner_text("#aguardando") == str(
        banco.valor("SELECT count(*) FROM pedidos WHERE status = 'AguardandoPagamento'"))
    assert admin.inner_text("#esgotados") == str(banco.valor("""
        SELECT count(*) FROM produtos p JOIN jogos j ON j.id = p.jogo_id JOIN plataformas pl ON pl.id = p.plataforma_id
        WHERE p.ativo AND j.ativo AND pl.ativa
          AND NOT EXISTS (SELECT 1 FROM chaves c WHERE c.produto_id = p.id AND c.status = 'Disponivel')"""))


def test_periodo_invalido_e_periodo_sem_vendas(paginas, banco):
    admin = paginas.admin()
    admin.goto("/Admin?dias=5")
    assert admin.locator("a[aria-current=true]").inner_text().strip() == "Últimos 30 dias"

    # empurra todas as vendas para longe no passado e depois devolve
    deslocar = "UPDATE pedidos SET pago_em = pago_em {} interval '400 days', criado_em = criado_em {} interval '400 days'"
    banco.executar(deslocar.format("-", "-"))
    try:
        admin.goto("/Admin?dias=7")
        assert "Nenhuma venda nos últimos 7 dias." in admin.inner_text("main")
        assert admin.inner_text("#faturamento") == brl(0) and admin.locator("#tabelaMaisVendidos").count() == 0
    finally:
        banco.executar(deslocar.format("+", "+"))
