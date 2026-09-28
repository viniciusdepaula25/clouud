"""Vitrine pública: filtros, selos, esgotados por último e botão de compra."""
from urllib.parse import unquote

import pytest

from conftest import LOJA, brl, caminho, enviar


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
    pagina.goto(f"{LOJA}?busca={prefixo}")
    assert len(titulos(pagina)) == 3
    pagina.goto(f"{LOJA}?busca={prefixo}&plataforma=epic-games")
    assert titulos(pagina) == [luta["titulo"]]
    pagina.goto(f"{LOJA}?busca={prefixo}&categoria=rpg")
    assert titulos(pagina) == [rpg["titulo"]]
    pagina.goto(f"{LOJA}?busca={prefixo}&promocoes=true")
    assert titulos(pagina) == [luta["titulo"]]
    card = pagina.locator(".card").first
    assert "-25%" in card.inner_text() and brl(150) in card.inner_text() and brl(200) in card.inner_text()


def test_esgotado_vai_para_o_fim_mesmo_em_destaque(paginas, fabrica):
    prefixo, rpg, luta, esgotado = criar_vitrine(fabrica)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}")
    assert titulos(pagina)[-1] == esgotado["titulo"]
    ultimo = pagina.locator(".card").last
    assert ultimo.locator("button[disabled]", has_text="Esgotado").count() == 1
    assert "Esgotado" in ultimo.locator(".badge").all_inner_texts()


def test_card_leva_a_pagina_do_jogo_e_capa_ausente_usa_imagem_padrao(paginas, fabrica):
    prefixo, rpg, *_ = criar_vitrine(fabrica)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={rpg['titulo']}")
    assert pagina.locator(".card img").first.get_attribute("src").endswith("sem-capa.svg")
    with pagina.expect_navigation():
        pagina.locator(".card-header a").first.click()
    assert pagina.url.endswith(f"/jogo/{rpg['slug']}")


def test_visitante_compra_pelo_login_e_volta_para_a_loja(paginas, fabrica):
    prefixo, rpg, *_ = criar_vitrine(fabrica)
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={rpg['titulo']}")
    with pagina.expect_navigation():
        pagina.locator(".card a", has_text="Comprar").first.click()
    assert "returnUrl=%2Floja%3Fbusca%3D" in pagina.url  # volta para a mesma busca depois do login
    pagina.fill("input[name=Email]", cliente["email"])
    pagina.fill("input[name=Senha]", cliente["senha"])
    enviar(pagina, "form input[type=submit] >> nth=-1")
    assert caminho(pagina) == LOJA and rpg["titulo"] in unquote(pagina.url)
    assert pagina.locator(".card button", has_text="Comprar").count() == 1  # agora já compra


def criar_para_ordenar(fabrica, banco):
    prefixo = f"Ordem{fabrica.sufixo()}"
    cliente = fabrica.cliente()
    a = fabrica.jogo(f"{prefixo} A", preco=50, chaves=3)
    b = fabrica.jogo(f"{prefixo} B", preco=200, promocional=30, chaves=3)
    c = fabrica.jogo(f"{prefixo} C", preco=100, chaves=3)
    d = fabrica.jogo(f"{prefixo} D", preco=10, chaves=0)  # esgotado: sempre por último
    banco.executar("UPDATE jogos SET data_lancamento = '2020-05-01' WHERE id = %s", a["jogo_id"])
    banco.executar("UPDATE jogos SET data_lancamento = '2024-03-01' WHERE id = %s", b["jogo_id"])
    banco.executar("UPDATE jogos SET data_lancamento = '2025-01-01' WHERE id = %s", d["jogo_id"])
    fabrica.pedido_pago(cliente["id"], b["produto_id"], quantidade=5)
    fabrica.pedido_pago(cliente["id"], a["produto_id"], quantidade=1)
    fabrica.pedido_pago(cliente["id"], c["produto_id"], quantidade=9, status="Reembolsado")  # não conta
    return prefixo, a, b, c, d


@pytest.mark.parametrize("ordem, esperado", [
    ("menor-preco", "BACD"),
    ("maior-preco", "CABD"),
    ("lancamentos", "BACD"),
    ("mais-vendidos", "BACD"),
    ("destaques", "ABCD"),
    ("qualquer-coisa", "ABCD"),  # ordem desconhecida vira "destaques"
])
def test_ordenacao(paginas, fabrica, banco, ordem, esperado):
    prefixo, *_ = criar_para_ordenar(fabrica, banco)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}&ordem={ordem}")
    assert "".join(t[-1] for t in titulos(pagina)) == esperado


def test_ordenar_pelo_seletor_mantem_a_busca(paginas, fabrica, banco):
    prefixo, *_ = criar_para_ordenar(fabrica, banco)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}")
    with pagina.expect_navigation():
        pagina.select_option("#ordem", "menor-preco")  # muda sozinho, sem clicar em Buscar
    assert "ordem=menor-preco" in pagina.url and prefixo in pagina.url
    assert "".join(t[-1] for t in titulos(pagina)) == "BACD"


@pytest.mark.parametrize("consulta, esperado", [
    ("precoMin=40&precoMax=150", "AC"),        # B custa 200, mas está por 30: vale o preço de agora
    ("precoMax=100,00", "ABCD"),               # vírgula no preço, até R$ 100 inclui o C
    ("precoMin=150&precoMax=40", "AC"),        # invertido é corrigido
    ("precoMin=100", "C"),
    ("precoMin=abc", "ABCD"),                  # valor inválido é ignorado
])
def test_faixa_de_preco(paginas, fabrica, banco, consulta, esperado):
    prefixo, *_ = criar_para_ordenar(fabrica, banco)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}&{consulta}")
    assert "".join(t[-1] for t in titulos(pagina)) == esperado


def test_faixa_de_preco_pelo_formulario(paginas, fabrica, banco):
    prefixo, *_ = criar_para_ordenar(fabrica, banco)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}")
    pagina.fill("#precoMin", "40")
    pagina.fill("#precoMax", "150,5")
    enviar(pagina, "#filtrosLoja input[type=submit]")
    assert "".join(t[-1] for t in titulos(pagina)) == "AC"
    assert pagina.input_value("#precoMin") == "40,00" and pagina.input_value("#precoMax") == "150,50"
    enviar(pagina, "text=Limpar filtros")
    assert "busca" not in pagina.url and pagina.input_value("#precoMin") == ""


def test_paginas(paginas, fabrica):
    prefixo = f"Pag{fabrica.sufixo()}"
    for i in range(30):
        fabrica.jogo(f"{prefixo} {i:02d}", preco=10 + i, chaves=1)
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={prefixo}&ordem=menor-preco")
    assert len(titulos(pagina)) == 24
    assert "30 produtos encontrados · mostrando 1–24" in pagina.inner_text("#resumoLoja")
    assert pagina.locator(".pagination .active").inner_text().strip() == "1"
    enviar(pagina, ".pagination a:has-text('Próxima')")
    assert "pagina=2" in pagina.url and "ordem=menor-preco" in pagina.url and prefixo in pagina.url
    assert titulos(pagina) == [f"{prefixo} {i:02d}" for i in range(24, 30)]
    assert "mostrando 25–30" in pagina.inner_text("#resumoLoja")
    assert pagina.locator(".pagination .page-item.disabled", has_text="Próxima").count() == 1

    pagina.goto(f"{LOJA}?busca={prefixo}&pagina=99")  # página que não existe vai para a última
    assert len(titulos(pagina)) == 6
    pagina.goto(f"{LOJA}?busca={prefixo}&pagina=0")
    assert len(titulos(pagina)) == 24

    pagina.goto(f"{LOJA}?busca={prefixo}Nada")
    assert pagina.locator(".pagination").count() == 0 and "Nenhum jogo encontrado." in pagina.inner_text("main")
