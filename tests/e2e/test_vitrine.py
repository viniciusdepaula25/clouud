"""Vitrine pública: filtros, selos, esgotados por último e botão de compra."""
from urllib.parse import unquote

from conftest import brl, caminho, enviar


def criar_vitrine(fabrica):
    prefixo = f"Vitrine{fabrica.sufixo()}"
    rpg = fabrica.jogo(f"{prefixo} Alfa", "steam", 100, chaves=2, categorias=("rpg",))
    luta = fabrica.jogo(f"{prefixo} Beta", "epic-games", 200, chaves=1, promocional=150, categorias=("luta",))
    esgotado = fabrica.jogo(f"{prefixo} Gama", "gog", 50, chaves=0, destaque=True)
    return prefixo, rpg, luta, esgotado


def titulos(pagina):
    return [t.strip() for t in pagina.locator(".card .card-header").all_inner_texts()]


def test_filtros_de_busca_plataforma_categoria_e_promocao(paginas, fabrica):
    prefixo, rpg, luta, esgotado = criar_vitrine(fabrica)
    pagina = paginas.nova()
    pagina.goto(f"/?busca={prefixo}")
    assert len(titulos(pagina)) == 3
    pagina.goto(f"/?busca={prefixo}&plataforma=epic-games")
    assert titulos(pagina) == [luta["titulo"]]
    pagina.goto(f"/?busca={prefixo}&categoria=rpg")
    assert titulos(pagina) == [rpg["titulo"]]
    pagina.goto(f"/?busca={prefixo}&promocoes=true")
    assert titulos(pagina) == [luta["titulo"]]
    card = pagina.locator(".card").first
    assert "-25%" in card.inner_text() and brl(150) in card.inner_text() and brl(200) in card.inner_text()


def test_esgotado_vai_para_o_fim_mesmo_em_destaque(paginas, fabrica):
    prefixo, rpg, luta, esgotado = criar_vitrine(fabrica)
    pagina = paginas.nova()
    pagina.goto(f"/?busca={prefixo}")
    assert titulos(pagina)[-1] == esgotado["titulo"]
    ultimo = pagina.locator(".card").last
    assert ultimo.locator("button[disabled]", has_text="Esgotado").count() == 1
    assert "Esgotado" in ultimo.locator(".badge").all_inner_texts()


def test_card_leva_a_pagina_do_jogo_e_capa_ausente_usa_imagem_padrao(paginas, fabrica):
    prefixo, rpg, *_ = criar_vitrine(fabrica)
    pagina = paginas.nova()
    pagina.goto(f"/?busca={rpg['titulo']}")
    assert pagina.locator(".card img").first.get_attribute("src").endswith("sem-capa.svg")
    with pagina.expect_navigation():
        pagina.locator(".card-header a").first.click()
    assert pagina.url.endswith(f"/jogo/{rpg['slug']}")


def test_visitante_compra_pelo_login_e_volta_para_a_loja(paginas, fabrica):
    prefixo, rpg, *_ = criar_vitrine(fabrica)
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    pagina.goto(f"/?busca={rpg['titulo']}")
    with pagina.expect_navigation():
        pagina.locator(".card a", has_text="Comprar").first.click()
    assert "returnUrl=%2F%3Fbusca%3D" in pagina.url  # volta para a mesma busca depois do login
    pagina.fill("input[name=Email]", cliente["email"])
    pagina.fill("input[name=Senha]", cliente["senha"])
    enviar(pagina, "form input[type=submit] >> nth=-1")
    assert caminho(pagina) == "/" and rpg["titulo"] in unquote(pagina.url)
    assert pagina.locator(".card button", has_text="Comprar").count() == 1  # agora já compra
