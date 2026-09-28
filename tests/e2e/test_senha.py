"""Esqueci minha senha: link por e-mail, validade, uso único e limite de pedidos."""
import hashlib
from urllib.parse import parse_qs, urlparse

from conftest import caminho, entrar, enviar


def pedir_link(pagina, email):
    pagina.goto("/conta/login")
    enviar(pagina, "text=Esqueci minha senha")
    pagina.fill("#Email", email)
    enviar(pagina, "input[value='Enviar link']")
    assert "Se existir uma conta com" in pagina.inner_text("#pedidoEnviado")


def token_do(link):
    return parse_qs(urlparse(link).query)["token"][0]


def test_redefinir_senha_pelo_email(paginas, fabrica, banco, caixa):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    pedir_link(pagina, cliente["email"].upper())  # maiúsculas não atrapalham

    [mensagem] = caixa.esperar(cliente["email"], "Redefinir sua senha")
    link = caixa.link(mensagem, "/conta/redefinir-senha")
    assert "1 hora" in mensagem["texto"] and link in mensagem["texto"]
    # no banco fica só o hash do código, e o conteúdo do e-mail é apagado depois do envio
    assert banco.valor("SELECT token_hash FROM redefinicoes_senha WHERE usuario_id = %s", cliente["id"]) == \
        hashlib.sha256(token_do(link).encode()).hexdigest()
    assert banco.valor("SELECT count(*) FROM emails WHERE usuario_id = %s AND html IS NOT NULL", cliente["id"]) == 0

    resposta = pagina.goto(link)
    assert resposta.headers.get("referrer-policy") == "no-referrer"
    assert cliente["email"] in pagina.inner_text("main")
    pagina.fill("#NovaSenha", "novasenha1")
    pagina.fill("#ConfirmarSenha", "diferente")
    pagina.locator("#ConfirmarSenha").blur()
    assert "As senhas não conferem" in pagina.inner_text("main")
    pagina.fill("#ConfirmarSenha", "novasenha1")
    pagina.locator("#ConfirmarSenha").blur()  # a mensagem de erro some e o botão sobe; só então clica
    enviar(pagina, "input[value='Salvar nova senha']")
    assert caminho(pagina) == "/conta/login" and "Senha alterada" in pagina.inner_text(".alert-success")

    caixa.esperar(cliente["email"], "foi alterada")
    antiga = paginas.nova()
    entrar(antiga, cliente["email"], cliente["senha"])
    assert "Usuário ou senha inválidos" in antiga.inner_text("main")
    nova = paginas.nova()
    entrar(nova, cliente["email"], "novasenha1")
    assert caminho(nova) == "/" and "Sair" in nova.inner_text("nav")
    # clicou no link do e-mail: o endereço fica confirmado
    assert banco.valor("SELECT email_confirmado_em IS NOT NULL FROM usuarios WHERE id = %s", cliente["id"])

    pagina.goto(link)  # o mesmo link não vale duas vezes
    assert "Link inválido ou vencido" in pagina.inner_text("#linkInvalido")


def test_email_sem_conta_tem_a_mesma_resposta_e_nao_recebe_nada(paginas, fabrica, caixa, banco):
    email = f"ninguem.{fabrica.sufixo()}@teste.com"
    pedir_link(paginas.nova(), email)
    cliente = fabrica.cliente()
    pedir_link(paginas.nova(), cliente["email"])
    caixa.esperar(cliente["email"], "Redefinir")  # o e-mail de quem tem conta já saiu...
    assert caixa.de(email) == []                   # ...e o sem conta não recebeu nada


def test_link_vencido_alterado_e_pedidos_antigos(paginas, fabrica, banco, caixa):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    pedir_link(pagina, cliente["email"])
    pedir_link(pagina, cliente["email"])
    primeiro, segundo = [caixa.link(m, "/conta/redefinir-senha")
                         for m in caixa.esperar(cliente["email"], "Redefinir", quantidade=2)]

    pagina.goto(primeiro[:-4] + "AAAA")
    assert "Link inválido" in pagina.inner_text("main")
    pagina.goto("/conta/redefinir-senha")
    assert "Link inválido" in pagina.inner_text("main")

    # POST direto com código inválido também não troca nada
    pagina.goto(segundo)
    pagina.evaluate("document.querySelector('input[name=Token]').value = 'inventado'")
    pagina.fill("#NovaSenha", "hackeada1")
    pagina.fill("#ConfirmarSenha", "hackeada1")
    enviar(pagina, "input[value='Salvar nova senha']")
    assert "Link inválido" in pagina.inner_text("main")

    # usar um link invalida os outros pedidos abertos
    pagina.goto(segundo)
    pagina.fill("#NovaSenha", "outrasenha1")
    pagina.fill("#ConfirmarSenha", "outrasenha1")
    enviar(pagina, "input[value='Salvar nova senha']")
    pagina.goto(primeiro)
    assert "Link inválido" in pagina.inner_text("main")

    # vencido (mais de 1 hora)
    pedir_link(pagina, cliente["email"])
    terceiro = caixa.link(caixa.esperar(cliente["email"], "Redefinir", quantidade=3)[-1], "/conta/redefinir-senha")
    banco.executar("UPDATE redefinicoes_senha SET expira_em = now() - interval '1 minute' WHERE token_hash = %s",
                   hashlib.sha256(token_do(terceiro).encode()).hexdigest())
    pagina.goto(terceiro)
    assert "Link inválido" in pagina.inner_text("main")


def test_limite_de_pedidos_por_hora(paginas, fabrica, banco, caixa):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    for _ in range(5):
        pedir_link(pagina, cliente["email"])  # a tela responde igual nas cinco vezes
    assert banco.valor("SELECT count(*) FROM redefinicoes_senha WHERE usuario_id = %s", cliente["id"]) == 3
    caixa.esperar(cliente["email"], "Redefinir", quantidade=3)
    assert len(caixa.de(cliente["email"])) == 3


def test_validacao_do_formulario(paginas):
    pagina = paginas.nova()
    pagina.goto("/conta/esqueci-senha")
    pagina.evaluate("document.querySelector('main form').noValidate = true")
    pagina.fill("#Email", "nao-e-email")
    with pagina.expect_navigation():
        pagina.evaluate("document.querySelector('main form').submit()")
    assert "Informe um e-mail válido" in pagina.inner_text("main")
    assert pagina.request.post("/conta/esqueci-senha", form={"Email": "x@y.com"}).status == 400  # sem antiforgery
