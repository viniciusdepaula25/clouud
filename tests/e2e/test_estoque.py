"""Admin > Estoque: importar chaves, duplicadas, inativar, reativar, excluir e o que não pode ser mexido."""
from conftest import enviar, token


def importar(admin, produto_id, texto):
    admin.goto(f"/Admin/Estoque/Importar/{produto_id}")
    admin.fill("#Codigos", texto)
    enviar(admin, "input[value=Importar]")
    return admin.inner_text("main")


def test_importar_ignora_vazias_repetidas_e_longas(paginas, fabrica, banco):
    jogo = fabrica.jogo()
    admin = paginas.admin()
    admin.goto(f"/Admin/Estoque/Importar/{jogo['produto_id']}")
    enviar(admin, "input[value=Importar]")
    assert "Cole pelo menos uma chave" in admin.inner_text("main")

    s = fabrica.sufixo().upper()
    texto = f"A{s}-1\n\n   B{s}-2   \nA{s}-1\r\nC{s}-3\r\n" + "X" * 201
    admin.fill("#Codigos", texto)
    assert "5 linha(s), 1 repetida(s)" in admin.inner_text("#contador")
    enviar(admin, "input[value=Importar]")
    t = admin.inner_text("main")
    assert "3 chaves importadas." in t
    assert "1 estavam repetidas no texto" in t and "1 passavam de 200" in t
    assert "3 disponíveis" in t
    assert f"A{s}-1" not in admin.locator(".codigo-mascarado").all_inner_texts()
    admin.check("#mostrarCodigos")
    assert admin.locator(".codigo-completo:visible", has_text=f"A{s}-1").count() == 1


def test_chave_nao_entra_em_dois_produtos(paginas, fabrica):
    um, outro = fabrica.jogo(), fabrica.jogo()
    codigo = f"UNICA-{fabrica.sufixo()}"
    admin = paginas.admin()
    assert "1 chave importada." in importar(admin, um["produto_id"], codigo)
    t = importar(admin, outro["produto_id"], f"{codigo}\nNOVA-{fabrica.sufixo()}")
    assert "1 chave importada." in t and "1 já estavam cadastradas" in t
    t = importar(admin, outro["produto_id"], codigo)
    assert "Nenhuma chave nova" in t and f"Já cadastradas: {codigo}" in t
    assert admin.input_value("#Codigos").strip() == codigo


def test_inativar_reativar_e_excluir(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=3)
    admin = paginas.admin()
    admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}")
    enviar(admin, "input[value=Inativar] >> nth=0")
    t = admin.inner_text("main")
    assert "Chave inativada." in t and "2 disponíveis" in t and "1 inativas" in t
    admin.click(".btn-group >> text=Inativa")
    admin.wait_for_load_state()
    assert admin.locator("tbody tr").count() == 1 and "status=Inativa" in admin.url
    enviar(admin, "input[value=Reativar]")
    assert "devolvida ao estoque" in admin.inner_text("main") and "status=Inativa" in admin.url
    admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}")
    enviar(admin, "input[value=Excluir] >> nth=0")
    assert "Chave excluída." in admin.inner_text("main") and "2 disponíveis" in admin.inner_text("main")


def test_chave_vendida_nao_pode_ser_alterada_nem_por_post_direto(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pedido = fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    item = banco.valor("SELECT id FROM pedido_itens WHERE pedido_id = %s", pedido)
    chave = banco.valor("SELECT id FROM chaves WHERE produto_id = %s", jogo["produto_id"])
    banco.executar("UPDATE chaves SET status = 'Vendida', pedido_item_id = %s WHERE id = %s", item, chave)

    admin = paginas.admin()
    admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}?status=Vendida")
    assert admin.locator("tbody tr").count() == 1 and admin.locator("tbody input[type=submit]").count() == 0
    admin.goto(f"/Admin/Estoque/Importar/{jogo['produto_id']}")  # a lista de vendidas não tem formulários
    antiforgery = token(admin)
    admin.request.post(f"/Admin/Estoque/Excluir/{chave}", form={"__RequestVerificationToken": antiforgery})
    admin.request.post(f"/Admin/Estoque/Inativar/{chave}", form={"__RequestVerificationToken": antiforgery})
    assert banco.valor("SELECT status FROM chaves WHERE id = %s", chave) == "Vendida"
    assert admin.request.post(f"/Admin/Estoque/Excluir/{chave}").status == 400  # sem token antiforgery


def test_produto_com_chaves_nao_e_excluido(paginas, fabrica):
    jogo = fabrica.jogo(chaves=1)
    admin = paginas.admin()
    admin.goto(f"/Admin/Produto/Exclui/{jogo['produto_id']}")
    enviar(admin, "input[value=Excluir]")
    assert "chave(s) no estoque e não pode ser excluído" in admin.inner_text("main")


def test_vitrine_mostra_esgotado_e_estoque_no_admin(paginas, fabrica):
    jogo = fabrica.jogo(chaves=0)
    admin = paginas.admin()
    admin.goto(f"/Admin/Estoque?busca={jogo['titulo']}")
    assert "Esgotado" in admin.inner_text("tbody")
    importar(admin, jogo["produto_id"], f"K-{fabrica.sufixo()}")
    vitrine = paginas.nova()
    vitrine.goto(f"/?busca={jogo['titulo']}")
    assert vitrine.locator(".card a", has_text="Comprar").count() == 1
