"""Lista de desejos: coração na vitrine, página da lista, promoção, esgotado, jogo fora da loja."""
from urllib.parse import unquote

from conftest import LOJA, brl, enviar


def card(pagina, titulo, plataforma=None):
    c = pagina.locator(".card", has=pagina.locator(".card-header", has_text=titulo))
    return c.filter(has=pagina.locator(".card-title", has_text=plataforma)) if plataforma else c


def coracoes(pagina, titulo):
    return card(pagina, titulo).locator("button.desejo").all_inner_texts()


def test_visitante_e_levado_ao_login(paginas, fabrica):
    jogo = fabrica.jogo(chaves=1)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={jogo['titulo']}")
    assert card(pagina, jogo["titulo"]).locator("a[aria-label='Entre para usar a lista de desejos']").count() == 1


def test_coracao_marca_o_jogo_em_todas_as_plataformas(paginas, fabrica, banco):
    jogo = fabrica.jogo(plataforma="steam", chaves=1)
    fabrica.produto(jogo["jogo_id"], "epic-games", 120)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    assert pagina.locator("nav a", has_text="Lista de desejos").inner_text().strip() == "Lista de desejos"

    pagina.goto(f"{LOJA}?busca={jogo['titulo']}")
    with pagina.expect_navigation():
        card(pagina, jogo["titulo"], "Steam").locator("button.desejo").click()
    assert unquote(pagina.url).endswith(f"{LOJA}?busca={jogo['titulo']}")  # volta para a mesma busca
    assert coracoes(pagina, jogo["titulo"]) == ["♥", "♥"]
    assert card(pagina, jogo["titulo"]).locator("button.desejo").first.get_attribute("aria-pressed") == "true"
    assert "Lista de desejos (1)" in pagina.locator("nav a", has_text="Lista de desejos").inner_text()

    with pagina.expect_navigation():
        card(pagina, jogo["titulo"], "Epic").locator("button.desejo").click()
    assert coracoes(pagina, jogo["titulo"]) == ["♡", "♡"]
    assert banco.valor("SELECT count(*) FROM lista_desejos WHERE usuario_id = %s", cliente["id"]) == 0


def test_pagina_da_lista_promocao_esgotado_e_fora_da_loja(paginas, fabrica, banco):
    promo = fabrica.jogo(preco=250, promocional=200, chaves=2)
    fabrica.produto(promo["jogo_id"], "epic-games", 300)
    esgotado = fabrica.jogo(chaves=0)
    inativo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    for j in (promo, esgotado, inativo):
        banco.executar("INSERT INTO lista_desejos (usuario_id, jogo_id, adicionado_em) VALUES (%s, %s, now())",
                       cliente["id"], j["jogo_id"])
    banco.executar("UPDATE jogos SET ativo = false WHERE id = %s", inativo["jogo_id"])

    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/ListaDesejos")
    assert pagina.locator("main .card").count() == 3
    texto = pagina.locator(".card", has_text=promo["titulo"]).inner_text()
    assert "Em promoção" in texto and "-20%" in texto and f"a partir de {brl(200)}" in texto
    assert pagina.locator(".card", has_text=esgotado["titulo"]).locator("button[disabled]", has_text="Esgotado").count() == 1
    assert "não está à venda" in pagina.locator(".card", has_text=inativo["titulo"]).inner_text()

    with pagina.expect_navigation():
        pagina.locator(".card", has_text=promo["titulo"]).locator("button", has_text="Comprar").click()
    assert "/Cliente/Carrinho" in pagina.url and brl(200) in pagina.inner_text("main")

    pagina.goto("/Cliente/ListaDesejos")
    enviar(pagina, f".card:has-text('{esgotado['titulo']}') input[value=Remover]")
    assert "removido" in pagina.inner_text(".alert-success") and pagina.locator("main .card").count() == 2


def test_lista_e_por_cliente_e_admin_ve_a_procura(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=1)
    ana, bia = fabrica.cliente("Ana"), fabrica.cliente("Bia")
    banco.executar("INSERT INTO lista_desejos (usuario_id, jogo_id, adicionado_em) VALUES (%s, %s, now())", ana["id"], jogo["jogo_id"])

    outra = paginas.logada(bia["email"], bia["senha"])
    outra.goto("/Cliente/ListaDesejos")
    assert "Sua lista está vazia" in outra.inner_text("main")
    outra.goto(f"{LOJA}?busca={jogo['titulo']}")
    assert coracoes(outra, jogo["titulo"]) == ["♡"]
    assert outra.request.post("/Cliente/ListaDesejos/Alternar", form={"jogoId": str(jogo["jogo_id"])}).status == 400

    admin = paginas.admin()
    admin.goto("/Admin/Estoque")
    assert "♥ 1" in admin.locator("tbody tr", has_text=jogo["titulo"]).inner_text()


def test_excluir_jogo_tira_das_listas(paginas, fabrica, banco):
    jogo_id = banco.valor("INSERT INTO jogos (titulo, slug, ativo, destaque) VALUES (%s, %s, true, false) RETURNING id",
                          "Temporário", f"temporario-{fabrica.sufixo()}")
    cliente = fabrica.cliente()
    banco.executar("INSERT INTO lista_desejos (usuario_id, jogo_id, adicionado_em) VALUES (%s, %s, now())", cliente["id"], jogo_id)
    admin = paginas.admin()
    admin.goto(f"/Admin/Jogo/Exclui/{jogo_id}")
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main")
    assert banco.valor("SELECT count(*) FROM lista_desejos WHERE jogo_id = %s", jogo_id) == 0
