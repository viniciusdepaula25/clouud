"""Página do jogo e avaliações: só quem comprou avalia, média, distribuição, edição, exclusão e moderação."""
import pytest

from conftest import LOJA, enviar, entrar


@pytest.fixture
def cenario(fabrica):
    jogo = fabrica.jogo(preco=249.90, chaves=3)
    return jogo, f"/jogo/{jogo['slug']}"


def resumo(pagina):
    return pagina.inner_text("#resumoNotas")


def avaliar(pagina, nota, comentario=None, botao="Enviar avaliação"):
    pagina.check(f"#nota-{nota}", force=True)
    if comentario is not None:
        pagina.fill("textarea[name=Comentario]", comentario)
    enviar(pagina, f"input[value='{botao}']")


def test_pagina_do_jogo_para_visitante(paginas, fabrica, banco, cenario):
    jogo, url = cenario
    pagina = paginas.nova()
    pagina.goto(f"{LOJA}?busca={jogo['titulo']}")
    link = pagina.locator(".card-header a", has_text=jogo["titulo"])
    assert link.get_attribute("href") == url
    enviar(pagina, f".card-header a:has-text('{jogo['titulo']}')")
    texto = pagina.inner_text("body")
    assert pagina.url.endswith(url) and jogo["titulo"] in pagina.inner_text("h1")
    assert "Onde comprar" in texto and "Ainda sem avaliações" in texto and "para avaliar este jogo" in texto
    assert pagina.locator("#produtos a", has_text="Comprar").first.get_attribute("href").endswith(
        f"returnUrl=%2Fjogo%2F{jogo['slug']}")
    assert pagina.request.get("/jogo/nao-existe").status == 404
    inativo = fabrica.jogo()
    banco.executar("UPDATE jogos SET ativo = false WHERE id = %s", inativo["jogo_id"])
    assert pagina.request.get(f"/jogo/{inativo['slug']}").status == 404


def test_quem_comprou_avalia_edita_e_exclui(paginas, fabrica, banco, cenario):
    jogo, url = cenario
    cliente = fabrica.cliente()
    fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    pagina = paginas.nova()
    pagina.goto(url)
    pagina.click("text=Entrar")
    pagina.wait_for_load_state()
    entrar(pagina, cliente["email"], cliente["senha"], url=pagina.url)
    assert pagina.url.endswith(url)  # o login volta para a página do jogo
    assert pagina.locator("#formAvaliar").count() == 1 and "Avalie este jogo" in pagina.inner_text("#avaliar")

    antiforgery = pagina.locator("#formAvaliar input[name=__RequestVerificationToken]").get_attribute("value")
    pagina.request.post(f"{url}/avaliar", max_redirects=0,
                        form={"Nota": "9", "Comentario": "x", "__RequestVerificationToken": antiforgery})
    assert banco.valor("SELECT count(*) FROM avaliacoes WHERE jogo_id = %s", jogo["jogo_id"]) == 0  # nota fora de 1 a 5

    pagina.check("#nota-4", force=True)
    assert pagina.locator("#notaOpcoes label.acesa").count() == 4
    avaliar(pagina, 4, "<script>alert(1)</script> Muito bom!\nSegunda linha")
    assert "Obrigado pela avaliação!" in pagina.inner_text(".alert-success")
    assert "4.0" in resumo(pagina) and "(1 avaliação)" in resumo(pagina)
    assert "<script>alert(1)</script> Muito bom!" in pagina.locator(".avaliacao").first.inner_text()  # texto, não HTML

    pagina.goto(f"{LOJA}?busca={jogo['titulo']}")
    assert "4.0 (1)" in pagina.locator(".card", has_text=jogo["titulo"]).first.inner_text()

    pagina.goto(url)
    assert "Muito bom!" in pagina.input_value("textarea[name=Comentario]")
    avaliar(pagina, 5, botao="Atualizar avaliação")
    assert "Avaliação atualizada." in pagina.inner_text(".alert-success") and "5.0" in resumo(pagina)
    assert "(editada)" in pagina.inner_text(".avaliacao")
    assert banco.valor("SELECT count(*) FROM avaliacoes WHERE jogo_id = %s", jogo["jogo_id"]) == 1

    with pagina.expect_navigation():
        pagina.click("button.desejo")
    assert pagina.url.endswith(url)
    assert banco.valor("SELECT count(*) FROM lista_desejos WHERE usuario_id = %s", cliente["id"]) == 1

    enviar(pagina, "input[value='Excluir minha avaliação']")
    assert "excluída" in pagina.inner_text(".alert-success")
    assert banco.valor("SELECT count(*) FROM avaliacoes WHERE jogo_id = %s", jogo["jogo_id"]) == 0


def test_quem_nao_comprou_nao_avalia_e_media_com_varias(paginas, fabrica, banco, cenario):
    jogo, url = cenario
    curioso = fabrica.cliente()
    pagina = paginas.logada(curioso["email"], curioso["senha"])
    pagina.goto(url)
    assert "Só quem comprou este jogo pode avaliar" in pagina.inner_text("#avaliar")
    assert pagina.locator("#formAvaliar").count() == 0
    antiforgery = pagina.locator("input[name=__RequestVerificationToken]").first.get_attribute("value")
    pagina.request.post(f"{url}/avaliar", max_redirects=0,
                        form={"Nota": "1", "Comentario": "forçado", "__RequestVerificationToken": antiforgery})
    assert banco.valor("SELECT count(*) FROM avaliacoes WHERE jogo_id = %s", jogo["jogo_id"]) == 0

    pedido = fabrica.pedido_pago(curioso["id"], jogo["produto_id"], status="Reembolsado")
    pagina.goto(url)
    assert pagina.locator("#formAvaliar").count() == 0  # compra reembolsada não vale
    banco.executar("UPDATE pedidos SET status = 'Pago' WHERE id = %s", pedido)
    pagina.goto(url)
    avaliar(pagina, 2, "Não gostei")

    outro = fabrica.cliente()
    fabrica.pedido_pago(outro["id"], jogo["produto_id"])
    segunda = paginas.logada(outro["email"], outro["senha"])
    segunda.goto(url)
    avaliar(segunda, 5, "Adorei")
    assert "3.5" in resumo(segunda) and "(2 avaliações)" in resumo(segunda)
    distribuicao = [segunda.locator(".distribuicao > div").nth(i).inner_text().split()[-1] for i in range(5)]
    assert distribuicao == ["1", "0", "0", "1", "0"]  # notas 5, 4, 3, 2, 1


def test_admin_modera_avaliacoes(paginas, fabrica, banco, cenario):
    jogo, url = cenario
    cliente = fabrica.cliente()
    fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    texto = f"Comentário {fabrica.sufixo()}"
    banco.executar("""INSERT INTO avaliacoes (usuario_id, jogo_id, nota, comentario, criada_em)
                      VALUES (%s, %s, 2, %s, now())""", cliente["id"], jogo["jogo_id"], texto)
    admin = paginas.admin()
    assert admin.locator("nav a", has_text="Avaliações").count() == 1
    admin.goto(f"/Admin/Avaliacoes?nota=2&busca={texto}")
    assert admin.locator("tbody tr").count() == 1
    admin.goto(f"/Admin/Avaliacoes?nota=5&busca={texto}")
    assert "Nenhuma avaliação encontrada." in admin.inner_text("tbody")
    admin.goto(f"/Admin/Avaliacoes?busca={texto}")
    enviar(admin, "input[value=Excluir]")
    assert "excluída" in admin.inner_text(".alert-success")
    admin.goto(url)
    assert "Ainda sem avaliações" in resumo(admin)
