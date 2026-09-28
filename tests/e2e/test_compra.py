"""Compra: carrinho, pedido, pagamento simulado, entrega das chaves, cancelamento, prazo, reembolso."""
from conftest import LOJA, brl, enviar, token


def comprar_pela_vitrine(pagina, titulo):
    pagina.goto(f"{LOJA}?busca={titulo}")
    with pagina.expect_navigation():
        pagina.locator(".card", has=pagina.locator(".card-header", has_text=titulo)).locator("button", has_text="Comprar").click()


def finalizar(pagina) -> int:
    enviar(pagina, "input[value='Finalizar compra']")
    assert "/Cliente/Pedidos/Pagar/" in pagina.url, pagina.inner_text("main")
    return int(pagina.url.rsplit("/", 1)[1])


def chaves_do_pedido(banco, pedido, status):
    return banco.valor("""SELECT count(*) FROM chaves c JOIN pedido_itens i ON i.id = c.pedido_item_id
                          WHERE i.pedido_id = %s AND c.status = %s""", pedido, status)


def test_fluxo_completo_carrinho_pagamento_e_chaves(paginas, fabrica, banco):
    jogo = fabrica.jogo(preco=249.90, chaves=5)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    for link in ["Meus pedidos", "Minhas chaves", "Carrinho", "Lista de desejos"]:
        assert pagina.locator("nav a", has_text=link).count() == 1

    comprar_pela_vitrine(pagina, jogo["titulo"])
    assert "Produto adicionado" in pagina.inner_text("main")
    assert pagina.inner_text("#contadorCarrinho") == "1"
    comprar_pela_vitrine(pagina, jogo["titulo"])
    assert pagina.input_value("select[name=quantidade]") == "2"
    with pagina.expect_navigation():
        pagina.select_option("select[name=quantidade]", "3")
    assert pagina.inner_text("#totalCarrinho") == brl(749.70)

    pedido = finalizar(pagina)
    assert "faltam" in pagina.inner_text("#tempoRestante")
    assert chaves_do_pedido(banco, pedido, "Reservada") == 3
    assert banco.valor("SELECT count(*) FROM carrinho_itens WHERE usuario_id = %s", cliente["id"]) == 0
    assert float(banco.valor("SELECT total FROM pedidos WHERE id = %s", pedido)) == 749.70

    # recusado (simulação) volta para o pagamento
    pagina.check("#metodo-Cartao")
    enviar(pagina, "button[value=false]")
    assert "recusado" in pagina.inner_text(".alert-danger") and "/Pagar/" in pagina.url

    enviar(pagina, "button[value=true]")
    t = pagina.inner_text("main")
    assert "Pagamento aprovado" in t and "Como ativar" in t
    codigos = pagina.locator("code[id^=chave-]").all_inner_texts()
    assert len(codigos) == 3
    assert chaves_do_pedido(banco, pedido, "Vendida") == 3
    assert banco.linhas("SELECT status, metodo FROM pagamentos WHERE pedido_id = %s ORDER BY id", pedido) == [
        ("Recusado", "Cartao"), ("Aprovado", "Pix")]

    pagina.goto(f"/Cliente/Pedidos/Pagar/{pedido}")
    assert "/Detalhes/" in pagina.url  # pedido pago não volta para o pagamento
    pagina.goto("/Cliente/MinhasChaves")
    assert all(c in pagina.inner_text("main") for c in codigos)


def test_esgotado_nao_entra_no_carrinho(paginas, fabrica):
    jogo = fabrica.jogo(chaves=0)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto(f"{LOJA}?busca={jogo['titulo']}")
    assert pagina.locator(".card button[disabled]", has_text="Esgotado").count() == 1
    resposta = pagina.request.post("/Cliente/Carrinho/Adicionar", form={
        "produtoId": str(jogo["produto_id"]), "voltarPara": LOJA, "__RequestVerificationToken": token(pagina)})
    assert "esgotado" in resposta.text().lower()


def test_cancelar_devolve_as_chaves(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=2)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    comprar_pela_vitrine(pagina, jogo["titulo"])
    pedido = finalizar(pagina)
    assert chaves_do_pedido(banco, pedido, "Reservada") == 1
    enviar(pagina, "input[value='Cancelar pedido']")
    assert "Pedido cancelado." in pagina.inner_text("main")
    assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Disponivel'", jogo["produto_id"]) == 2


def test_prazo_vencido_cancela_ao_tentar_pagar(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    comprar_pela_vitrine(pagina, jogo["titulo"])
    pedido = finalizar(pagina)
    banco.executar("UPDATE pedidos SET pagar_ate = now() - interval '1 minute' WHERE id = %s", pedido)
    enviar(pagina, "button[value=true]")
    assert "prazo para pagar terminou" in pagina.inner_text("main")
    assert banco.valor("SELECT status FROM pedidos WHERE id = %s", pedido) == "Cancelado"
    assert chaves_do_pedido(banco, pedido, "Reservada") == 0


def test_admin_reembolsa_e_chaves_ficam_inativas(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=2)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    comprar_pela_vitrine(pagina, jogo["titulo"])
    pedido = finalizar(pagina)
    enviar(pagina, "button[value=true]")
    codigo = pagina.locator("code[id^=chave-]").first.inner_text()

    admin = paginas.admin()
    admin.goto(f"/Admin/Pedidos/Detalhes/{pedido}")
    assert cliente["email"] in admin.inner_text("main") and codigo not in admin.inner_text("main")  # mascarada
    enviar(admin, "input[value=Reembolsar]")
    assert "Reembolsado" in admin.inner_text("h1")
    assert chaves_do_pedido(banco, pedido, "Inativa") == 1

    admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}?status=Inativa")
    linha = admin.locator("tbody tr", has=admin.locator(f"a:text('#{pedido}')"))
    assert linha.count() == 1 and linha.locator("input[type=submit]").count() == 0
    pagina.goto("/Cliente/MinhasChaves")
    assert codigo not in pagina.inner_text("main")
    pagina.goto(f"/Cliente/Pedidos/Detalhes/{pedido}")
    assert "reembolsado" in pagina.inner_text("main") and pagina.locator("code[id^=chave-]").count() == 0


def test_admin_cancela_e_cliente_nao_paga_mais(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    comprar_pela_vitrine(pagina, jogo["titulo"])
    pedido = finalizar(pagina)
    admin = paginas.admin()
    admin.goto(f"/Admin/Pedidos/Detalhes/{pedido}")
    enviar(admin, "input[value='Cancelar pedido']")
    assert banco.valor("SELECT status FROM pedidos WHERE id = %s", pedido) == "Cancelado"
    enviar(pagina, "button[value=true]")
    assert "não está mais aguardando" in pagina.inner_text("main")


def test_cliente_nao_ve_pedido_de_outro(paginas, fabrica):
    jogo = fabrica.jogo()
    dono, outro = fabrica.cliente(), fabrica.cliente()
    pedido = fabrica.pedido_pago(dono["id"], jogo["produto_id"])
    pagina = paginas.logada(outro["email"], outro["senha"])
    assert pagina.request.get(f"/Cliente/Pedidos/Detalhes/{pedido}").status == 404


def test_pedido_antigo_sem_chave_e_explicado(paginas, fabrica):
    jogo = fabrica.jogo()
    cliente = fabrica.cliente()
    pedido = fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto(f"/Cliente/Pedidos/Detalhes/{pedido}")
    assert "antes da entrega automática" in pagina.inner_text("main")


def test_admin_lista_filtra_e_busca_pedidos(paginas, fabrica):
    jogo = fabrica.jogo()
    cliente = fabrica.cliente()
    pedido = fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    admin = paginas.admin()
    admin.goto("/Admin/Pedidos?status=Pago")
    assert "Pago" in admin.inner_text("tbody")
    admin.fill("#txtBusca", f"#{pedido}")
    enviar(admin, "input[value=Buscar]")
    assert admin.locator("tbody tr").count() == 1
    admin.goto(f"/Admin/Pedidos?busca={cliente['email']}")
    assert admin.locator("tbody tr").count() == 1
