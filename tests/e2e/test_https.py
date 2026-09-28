"""HTTPS, HSTS, cabeçalhos de segurança e Content-Security-Policy."""
import html
import re

import pytest
import requests

from conftest import SAIDA, Aplicacao, ambiente_da_aplicacao


def test_cabecalhos_de_seguranca(app):
    r = requests.get(app + "/loja", timeout=10)
    h = r.headers
    assert h["X-Content-Type-Options"] == "nosniff"
    assert h["X-Frame-Options"] == "DENY"
    assert h["Referrer-Policy"] == "strict-origin-when-cross-origin"
    assert "camera=()" in h["Permissions-Policy"]
    assert "Server" not in h  # não anuncia o servidor (Kestrel)
    csp = h["Content-Security-Policy"]
    for diretiva in ("default-src 'self'", "object-src 'none'", "frame-ancestors 'none'", "form-action 'self'", "base-uri 'self'"):
        assert diretiva in csp
    assert re.search(r"script-src 'self' 'nonce-[A-Za-z0-9+/=]+'", csp)
    assert "unsafe-inline" not in csp.split("script-src")[1].split(";")[0]  # script inline só com nonce


def test_nonce_muda_a_cada_resposta_e_vai_nos_scripts_da_pagina(paginas):
    admin = paginas.admin()
    resposta = admin.goto("/Admin")  # o painel tem script escrito na própria página
    nonce = re.search(r"'nonce-([^']+)'", resposta.headers["content-security-policy"]).group(1)
    scripts = admin.locator("script")
    assert scripts.count() > 0
    for i in range(scripts.count()):
        assert scripts.nth(i).evaluate("s => s.nonce") == nonce
    outra = admin.request.get("/Admin").headers["content-security-policy"]
    assert nonce not in outra
    # a dica do gráfico (script com nonce) funciona
    if admin.locator("#graficoVendas .coluna").count():
        admin.locator("#graficoVendas .coluna").last.hover()
        assert admin.locator("#tooltipVendas").is_visible()


def test_script_injetado_nao_executa(browser, app, fabrica, banco):
    """Mesmo que um texto com <script> chegasse ao HTML, o navegador não executaria (sem o nonce)."""
    contexto = browser.new_context(base_url=app)
    try:
        pagina = contexto.new_page()
        bloqueios = []
        pagina.on("console", lambda m: "Content Security Policy" in m.text and bloqueios.append(m.text))
        pagina.goto("/loja")
        pagina.evaluate("""() => {
            const s = document.createElement('script');
            s.textContent = 'window.invadido = true';
            document.body.appendChild(s);
            const img = document.createElement('img');
            img.setAttribute('onerror', 'window.invadido2 = true');
            img.src = '/nao-existe.png';
            document.body.appendChild(img);
        }""")
        pagina.wait_for_timeout(500)
        assert pagina.evaluate("window.invadido === undefined && window.invadido2 === undefined")
        assert bloqueios
    finally:
        contexto.close()


def test_imagem_reserva_confirmacao_e_ordem_sem_javascript_inline(paginas, fabrica, banco):
    """Os antigos onerror/onsubmit/onchange viraram data-* tratados pelo site.js."""
    jogo = fabrica.jogo(chaves=1)
    banco.executar("UPDATE jogos SET capa = 'capas/nao-existe.png' WHERE id = %s", jogo["jogo_id"])
    pagina = paginas.nova()
    pagina.goto(f"/loja?busca={jogo['titulo']}")
    capa = pagina.locator(".card img").first
    pagina.wait_for_function("img => img.src.endsWith('sem-capa.svg')", arg=capa.element_handle())
    with pagina.expect_navigation():
        pagina.select_option("#ordem", "menor-preco")
    assert "ordem=menor-preco" in pagina.url

    admin = paginas.admin()
    admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}")
    perguntas = []
    admin.on("dialog", lambda d: perguntas.append(d.message))
    with admin.expect_navigation():
        admin.locator("input[value=Excluir]").first.click()
    assert perguntas == ["Excluir esta chave do estoque?"]


def test_nenhuma_view_tem_javascript_inline():
    from conftest import PROJETO
    atributos = re.compile(r"\son(error|click|submit|change|load|input|keyup|keydown)\s*=", re.I)
    achados = [str(p.relative_to(PROJETO)) for p in PROJETO.rglob("*.cshtml")
               if "Emails" not in p.parts and "bin" not in p.parts and "obj" not in p.parts and atributos.search(p.read_text())]
    assert achados == []


@pytest.fixture(scope="module")
def producao(app):
    """A aplicação em modo produção, com HTTPS forçado, atrás de um proxy (simulado pelos cabeçalhos X-Forwarded-*)."""
    url = "http://127.0.0.1:5104"
    ambiente = ambiente_da_aplicacao(url, ASPNETCORE_ENVIRONMENT="Production", Seguranca__Https__Forcar="true",
                                     Seguranca__Proxy__Confiar="true", https_port="5443")
    aplicacao = Aplicacao(url, ambiente, SAIDA / "app-producao.log")
    try:
        assert aplicacao.esperar()
        yield url
    finally:
        aplicacao.parar()


@pytest.mark.lento
def test_http_redireciona_para_https(producao):
    r = requests.get(producao + "/loja?busca=x", allow_redirects=False, timeout=10)
    assert r.status_code in (307, 308)
    assert r.headers["Location"] == "https://127.0.0.1:5443/loja?busca=x"


@pytest.mark.lento
def test_via_proxy_https_manda_hsts_e_cookies_seguros(producao):
    r = requests.get(producao + "/conta/login", timeout=10, allow_redirects=False,
                     headers={"X-Forwarded-Proto": "https", "Host": "loja.exemplo.com.br"})
    assert r.status_code == 200
    assert r.headers["Strict-Transport-Security"] == "max-age=31536000; includeSubDomains"
    assert "upgrade-insecure-requests" in r.headers["Content-Security-Policy"]
    cookie = r.headers["Set-Cookie"].lower()
    assert "clouud.antiforgery" in cookie and "secure" in cookie and "httponly" in cookie


@pytest.mark.lento
def test_erro_em_producao_nao_mostra_detalhes(producao):
    r = requests.get(producao + "/pagina-inexistente", timeout=10,
                     headers={"X-Forwarded-Proto": "https", "Host": "loja.exemplo.com.br"})
    texto = html.unescape(r.content.decode("utf-8"))  # o Razor escreve acentos como &#xE1;
    assert r.status_code == 404 and "Página não encontrada" in texto
    assert "Exception" not in texto and "StackTrace" not in texto
