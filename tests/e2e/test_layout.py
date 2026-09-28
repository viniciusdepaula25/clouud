"""Layout: uma barra só (visitante e cliente), menu de celular e nenhum estilo global de link."""
import pytest

from conftest import LOJA, enviar

CELULAR = {"width": 390, "height": 844}


def test_mesma_barra_para_visitante_e_cliente(paginas, fabrica):
    jogo = fabrica.jogo(chaves=1)
    visitante = paginas.nova()
    for url in (LOJA, "/conta/login", "/conta/cadastro", f"/jogo/{jogo['slug']}"):
        visitante.goto(url)
        assert visitante.locator("nav.barra-loja").count() == 1, url
        assert visitante.locator("nav.barra-loja a", has_text="Entrar").count() == 1, url
        assert visitante.locator("footer.rodape").count() == 1, url

    cliente = fabrica.cliente()
    logada = paginas.logada(cliente["email"], cliente["senha"])
    for url in (LOJA, f"/jogo/{jogo['slug']}", "/Cliente/Pedidos", "/Cliente/Carrinho", "/Cliente/MinhaConta"):
        logada.goto(url)
        assert logada.locator("nav.barra-loja").count() == 1, url
        assert logada.locator("nav.barra-loja a", has_text="Meus pedidos").count() == 1, url
        assert logada.locator("nav a", has_text="Entrar").count() == 0, url


def test_busca_da_barra_leva_para_a_loja(paginas, fabrica):
    jogo = fabrica.jogo(chaves=1)
    pagina = paginas.nova()
    pagina.goto("/conta/login")
    pagina.fill("nav input[name=busca]", jogo["titulo"])
    with pagina.expect_navigation():
        pagina.press("nav input[name=busca]", "Enter")
    titulos = pagina.locator(".card .card-header").all_inner_texts()
    assert [t.strip() for t in titulos] == [jogo["titulo"]]


def test_endereco_antigo_da_loja_do_cliente_redireciona(paginas, fabrica):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto(f"/Cliente/Home?busca={jogo['titulo']}")
    assert pagina.locator(".card button", has_text="Comprar").count() == 1
    assert "/Cliente/Home" not in pagina.url


@pytest.mark.parametrize("quem", ["visitante", "cliente", "admin"])
def test_menu_no_celular(browser, app, fabrica, quem):
    contexto = browser.new_context(base_url=app, viewport=CELULAR)
    try:
        pagina = contexto.new_page()
        if quem != "visitante":
            conta = fabrica.cliente() if quem == "cliente" else {"email": "admin@clouud.com", "senha": "Admin@123"}
            pagina.goto("/conta/login")
            pagina.fill("input[name=Email]", conta["email"])
            pagina.fill("input[name=Senha]", conta["senha"])
            enviar(pagina, "form input[type=submit] >> nth=-1")
        pagina.goto("/Admin" if quem == "admin" else LOJA)
        botao = pagina.locator("nav .navbar-toggler")
        link = pagina.locator("nav .navbar-collapse a").first
        assert botao.is_visible() and not link.is_visible()
        botao.click()
        pagina.wait_for_timeout(500)  # animação do menu
        assert link.is_visible() and botao.get_attribute("aria-expanded") == "true"
        # nada passa da largura da tela (sem rolagem para o lado)
        assert pagina.evaluate("document.documentElement.scrollWidth") <= CELULAR["width"]
    finally:
        contexto.close()


def test_links_sem_margem_global(paginas, fabrica):
    """O antigo `a { margin-left }` empurrava todos os links; agora cada espaçamento é de propósito."""
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    admin = paginas.admin()
    for p, url in ((pagina, LOJA), (pagina, f"/jogo/{jogo['slug']}"), (pagina, "/Cliente/MinhaConta"),
                   (admin, "/Admin/Jogo"), (admin, "/Admin")):
        p.goto(url)
        margens = p.evaluate("""[...document.querySelectorAll('main a')]
            .map(a => getComputedStyle(a).marginLeft).filter(m => m === '15px' || m === '10px')""")
        assert margens == [], url
