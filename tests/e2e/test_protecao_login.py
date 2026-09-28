"""Proteções do login e dos formulários: bloqueio da conta, limites de uso, senhas fortes, senhas antigas."""
import time

import pytest
import requests

from conftest import (ADMIN_EMAIL, SAIDA, Aplicacao, ambiente_da_aplicacao, caminho, entrar, enviar, hash_senha)


def test_conta_bloqueada_depois_de_5_senhas_erradas(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    for _ in range(5):
        entrar(pagina, cliente["email"], "errada1234")
        assert "Usuário ou senha inválidos" in pagina.inner_text("main")
    assert banco.valor("SELECT bloqueado_ate > now() FROM usuarios WHERE id = %s", cliente["id"])

    entrar(pagina, cliente["email"], cliente["senha"])  # nem a senha certa entra durante o bloqueio
    assert "login desta conta está bloqueado" in pagina.inner_text("main") and caminho(pagina) != "/"

    banco.executar("UPDATE usuarios SET bloqueado_ate = now() - interval '1 second' WHERE id = %s", cliente["id"])
    entrar(pagina, cliente["email"], cliente["senha"])
    assert caminho(pagina) == "/"
    assert banco.linhas("SELECT falhas_login, bloqueado_ate FROM usuarios WHERE id = %s", cliente["id"]) == [(0, None)]


def test_login_certo_zera_as_falhas(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    for _ in range(4):
        entrar(pagina, cliente["email"], "errada1234")
    assert banco.valor("SELECT falhas_login FROM usuarios WHERE id = %s", cliente["id"]) == 4
    entrar(pagina, cliente["email"], cliente["senha"])
    assert banco.valor("SELECT falhas_login FROM usuarios WHERE id = %s", cliente["id"]) == 0


def test_email_inexistente_tem_a_mesma_resposta(paginas, fabrica):
    pagina = paginas.nova()
    entrar(pagina, f"ninguem.{fabrica.sufixo()}@teste.com", "qualquer1234")
    assert "Usuário ou senha inválidos" in pagina.inner_text("main")


@pytest.mark.parametrize("senha, mensagem", [
    ("curta1", "pelo menos 8 caracteres"),
    ("12345678", "muito comum"),
    ("Senha123", "muito comum"),
    ("aaaaaaaaaa", "muito comum"),
])
def test_senhas_fracas_sao_recusadas_no_cadastro(paginas, fabrica, senha, mensagem):
    pagina = paginas.nova()
    pagina.goto("/conta/cadastro")
    pagina.fill("#Nome", "Fraca")
    pagina.fill("#Email", f"fraca.{fabrica.sufixo()}@teste.com")
    pagina.fill("#Senha", senha)
    pagina.evaluate("document.querySelector('main form').noValidate = true")
    with pagina.expect_navigation():
        pagina.evaluate("document.querySelector('main form').submit()")
    assert mensagem in pagina.inner_text("main")


def test_email_invalido_no_cadastro(paginas):
    pagina = paginas.nova()
    pagina.goto("/conta/cadastro")
    pagina.fill("#Nome", "Sem Email")
    pagina.fill("#Email", "isso-nao-e-email")
    pagina.fill("#Senha", "segredo1234")
    pagina.evaluate("document.querySelector('main form').noValidate = true")
    with pagina.expect_navigation():
        pagina.evaluate("document.querySelector('main form').submit()")
    assert "Informe um e-mail válido" in pagina.inner_text("main")


def test_pagina_de_erro_404(paginas):
    pagina = paginas.nova()
    resposta = pagina.goto("/pagina-que-nao-existe")
    assert resposta.status == 404 and "Página não encontrada" in pagina.inner_text("#paginaErro")


def token_de(sessao, url):
    import re
    html = sessao.get(url).text
    return re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html).group(1)


@pytest.fixture(scope="module")
def app_com_limites(app):
    """Outra instância da aplicação, com limites baixos, para testar o rate limiting."""
    url = "http://127.0.0.1:5101"
    ambiente = ambiente_da_aplicacao(url, Seguranca__Limites__LoginPorMinuto="3",
                                     Seguranca__Limites__FormulariosDeContaPor15Minutos="2")
    aplicacao = Aplicacao(url, ambiente, SAIDA / "app-limites.log")
    try:
        assert aplicacao.esperar()
        yield url
    finally:
        aplicacao.parar()


@pytest.mark.lento
def test_limite_de_tentativas_de_login_por_ip(app_com_limites, fabrica):
    s = requests.Session()
    s.trust_env = False
    respostas = []
    for i in range(4):
        tok = token_de(s, app_com_limites + "/conta/login")
        r = s.post(app_com_limites + "/conta/login", allow_redirects=False,
                   data={"Email": f"robo{i}.{fabrica.sufixo()}@teste.com", "Senha": "chute12345", "__RequestVerificationToken": tok})
        respostas.append(r)
    assert [r.status_code for r in respostas[:3]] == [200, 200, 200]
    assert respostas[3].status_code == 429
    assert "Muitas tentativas" in respostas[3].text and int(respostas[3].headers["Retry-After"]) > 0


@pytest.mark.lento
def test_limite_de_cadastros_por_ip(app_com_limites, fabrica):
    s = requests.Session()
    s.trust_env = False
    codigos = []
    for i in range(3):
        tok = token_de(s, app_com_limites + "/conta/cadastro")
        r = s.post(app_com_limites + "/conta/cadastro", allow_redirects=False,
                   data={"Nome": "Robo", "Email": f"robo.cad{i}.{fabrica.sufixo()}@teste.com", "Senha": "robo-segredo-1",
                         "__RequestVerificationToken": tok})
        codigos.append(r.status_code)
        s.cookies.clear()  # sai da conta criada para tentar outra
    assert codigos == [302, 302, 429]


@pytest.mark.lento
def test_senhas_em_texto_puro_viram_hash_ao_iniciar(app, fabrica, banco):
    """Contas do projeto original tinham a senha aberta no banco; ao iniciar, a aplicação troca pelo hash."""
    email = f"antiga.{fabrica.sufixo()}@teste.com"
    usuario_id = banco.valor("INSERT INTO usuarios (name, email, senha, perfil) VALUES ('Antiga', %s, 'minhasenha1', 0) RETURNING id", email)
    url = "http://127.0.0.1:5102"
    outra = Aplicacao(url, ambiente_da_aplicacao(url), SAIDA / "app-senhas.log")
    try:
        assert outra.esperar()
        senha = banco.valor("SELECT senha FROM usuarios WHERE id = %s", usuario_id)
        assert senha != "minhasenha1" and "minhasenha1" not in senha
        assert "senha(s) em texto puro foram trocadas pelo hash" in outra.texto_do_log()
    finally:
        outra.parar()


def test_senha_em_texto_puro_nao_entra(paginas, fabrica, banco):
    """Se alguém gravar uma senha aberta direto no banco, ela não serve para entrar."""
    email = f"aberta.{fabrica.sufixo()}@teste.com"
    banco.executar("INSERT INTO usuarios (name, email, senha, perfil) VALUES ('Aberta', %s, 'senhaaberta1', 0)", email)
    pagina = paginas.nova()
    entrar(pagina, email, "senhaaberta1")
    assert "Usuário ou senha inválidos" in pagina.inner_text("main")


@pytest.mark.lento
def test_sem_senha_configurada_o_admin_inicial_nao_e_criado(app):
    from conftest import conectar, recriar_banco
    banco_vazio = "clouud_testes_sem_admin"
    recriar_banco(banco_vazio)
    url = "http://127.0.0.1:5103"
    outra = Aplicacao(url, ambiente_da_aplicacao(url, banco=banco_vazio, AdminInicial__Senha=""), SAIDA / "app-sem-admin.log")
    try:
        assert outra.esperar()
        time.sleep(1)
        with conectar(banco_vazio) as c:
            assert c.execute("SELECT count(*) FROM usuarios").fetchone()[0] == 0
        assert "Ainda não há administrador" in outra.texto_do_log()
    finally:
        outra.parar()
        with conectar("postgres") as c:
            c.execute(f'DROP DATABASE IF EXISTS "{banco_vazio}" WITH (FORCE)')
