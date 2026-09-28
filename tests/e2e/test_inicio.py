"""Página inicial: destaques, prateleiras de promoções, mais vendidos e lançamentos, atalhos de plataforma."""
import datetime

import pytest

from conftest import LOJA, caminho, enviar

HOJE = datetime.date.today()


def titulos(pagina, prateleira):
    return [t.strip() for t in pagina.locator(f"#{prateleira} .card .card-header").all_inner_texts()]


@pytest.fixture(scope="module")
def vitrine(fabrica, banco):
    """Dados que ganham de qualquer outro teste em cada prateleira."""
    s = fabrica.sufixo()
    cliente = fabrica.cliente()
    promo = fabrica.jogo(f"Promo {s}", preco=1000, promocional=5, chaves=2)          # 99,5% de desconto
    fabrica.produto(promo["jogo_id"], "epic-games", 1000, promocional=5)            # mesmo jogo em outra loja
    banco.executar("INSERT INTO chaves (produto_id, codigo, status, adicionada_em) SELECT id, %s, 'Disponivel', now() "
                   "FROM produtos WHERE jogo_id = %s AND plataforma_id = (SELECT id FROM plataformas WHERE slug = 'epic-games')",
                   f"EPIC{s}", promo["jogo_id"])
    esgotado = fabrica.jogo(f"Esgotado {s}", preco=1000, promocional=1, chaves=0)  # 99,9%, mas sem chave
    campeao = fabrica.jogo(f"Campeao {s}", preco=20, chaves=5)
    fabrica.pedido_pago(cliente["id"], campeao["produto_id"], quantidade=500, preco=20)
    novo = fabrica.jogo(f"Novo {s}", preco=90, chaves=1)
    futuro = fabrica.jogo(f"Futuro {s}", preco=90, chaves=1)
    banco.executar("UPDATE jogos SET data_lancamento = %s WHERE id = %s", HOJE, novo["jogo_id"])
    banco.executar("UPDATE jogos SET data_lancamento = %s WHERE id = %s", HOJE + datetime.timedelta(days=30), futuro["jogo_id"])
    destaque = fabrica.jogo(f"Destaque {s}", preco=150, promocional=100, chaves=1, destaque=True)
    return dict(promo=promo, esgotado=esgotado, campeao=campeao, novo=novo, futuro=futuro, destaque=destaque)


def test_prateleiras(paginas, vitrine):
    pagina = paginas.nova()
    pagina.goto("/")
    promocoes = titulos(pagina, "promocoes")
    assert promocoes[0] == vitrine["promo"]["titulo"]
    assert promocoes.count(vitrine["promo"]["titulo"]) == 1       # um produto por jogo
    assert vitrine["esgotado"]["titulo"] not in promocoes         # sem chave não entra
    assert len(promocoes) <= 4
    assert titulos(pagina, "mais-vendidos")[0] == vitrine["campeao"]["titulo"]
    lancamentos = titulos(pagina, "lancamentos")
    assert lancamentos[0] == vitrine["novo"]["titulo"] and vitrine["futuro"]["titulo"] not in lancamentos

    links = {p: pagina.locator(f"#{p} a.ver-todos").get_attribute("href")
             for p in ("promocoes", "mais-vendidos", "lancamentos")}
    assert links == {"promocoes": "/loja?promocoes=true", "mais-vendidos": "/loja?ordem=mais-vendidos",
                     "lancamentos": "/loja?ordem=lancamentos"}
    enviar(pagina, "#mais-vendidos a.ver-todos")
    assert caminho(pagina) == LOJA and pagina.input_value("#ordem") == "mais-vendidos"


def test_destaques_e_atalhos(paginas, vitrine):
    pagina = paginas.nova()
    pagina.goto("/")
    item = pagina.locator("#destaques .carousel-item", has_text=vitrine["destaque"]["titulo"])
    assert item.count() == 1 and "-33%" in item.inner_text()
    assert pagina.locator("#destaques .carousel-item").count() <= 5
    assert item.locator("a", has_text="Ver jogo").get_attribute("href") == f"/jogo/{vitrine['destaque']['slug']}"

    steam = pagina.locator(".atalhos-plataformas a", has_text="Steam")
    assert steam.get_attribute("href") == "/loja?plataforma=steam"
    enviar(pagina, ".atalhos-plataformas a:has-text('Epic Games')")
    assert pagina.input_value("#filtroPlataforma") == "epic-games"


def test_cliente_compra_e_favorita_pela_pagina_inicial(paginas, fabrica, banco, vitrine):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    assert caminho(pagina) == "/" and pagina.locator("#promocoes").count() == 1  # o login leva para a página inicial
    card = pagina.locator("#lancamentos .card", has_text=vitrine["novo"]["titulo"])
    with pagina.expect_navigation():
        card.locator("button.desejo").click()
    assert caminho(pagina) == "/"
    assert banco.valor("SELECT count(*) FROM lista_desejos WHERE usuario_id = %s", cliente["id"]) == 1
    with pagina.expect_navigation():
        pagina.locator("#lancamentos .card", has_text=vitrine["novo"]["titulo"]).locator("button", has_text="Comprar").click()
    assert "Produto adicionado" in pagina.inner_text("main")


def test_visitante_vai_para_o_login_e_volta(paginas, fabrica, vitrine):
    pagina = paginas.nova()
    pagina.goto("/")
    with pagina.expect_navigation():
        pagina.locator("#promocoes .card a", has_text="Comprar").first.click()
    assert "/conta/login" in pagina.url.lower()
