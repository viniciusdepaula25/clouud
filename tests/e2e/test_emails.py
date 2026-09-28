"""E-mails automáticos: fila, boas-vindas com confirmação do e-mail e registro no admin."""
import time

from conftest import caminho, enviar


def cadastrar(pagina, nome, email, senha="segredo1"):
    pagina.goto("/conta/cadastro")
    pagina.fill("#Nome", nome)
    pagina.fill("#Email", email)
    pagina.fill("#Senha", senha)
    enviar(pagina, "form input[type=submit]")


def test_cadastro_manda_boas_vindas_com_link_de_confirmacao(paginas, fabrica, banco, caixa):
    email = f"nova.{fabrica.sufixo()}@teste.com"
    pagina = paginas.nova()
    cadastrar(pagina, "Nova <b>Cliente</b>", email)
    assert f"Enviamos um e-mail para {email}" in pagina.inner_text(".alert-success")

    [mensagem] = caixa.esperar(email, "Bem-vindo")
    assert "CLOUUD" in mensagem["de"] and "nao-responda@" in mensagem["de"]
    assert "Nova &lt;b&gt;Cliente&lt;/b&gt;" in mensagem["html"]  # nome vai como texto, não como HTML
    assert "Nova <b>Cliente</b>" in mensagem["texto"] and "Confirmar meu e-mail" in mensagem["texto"]
    usuario_id = banco.valor("SELECT id FROM usuarios WHERE email = %s", email)
    assert banco.valor("SELECT email_confirmado_em FROM usuarios WHERE id = %s", usuario_id) is None

    # na fila fica só o registro; o conteúdo é apagado depois do envio
    assert banco.linhas("SELECT tipo, enviado_em IS NOT NULL, html, texto FROM emails WHERE usuario_id = %s",
                        usuario_id) == [("BoasVindas", True, None, None)]

    pagina.goto("/Cliente/MinhaConta")
    assert "E-mail não confirmado" in pagina.inner_text("#statusEmail")

    link = caixa.link(mensagem, "/conta/confirmar-email")
    assert link.startswith(paginas.url)  # endereço da configuração Loja:UrlPublica
    visitante = paginas.nova()  # o link funciona mesmo sem estar logado
    visitante.goto(link)
    assert "E-mail confirmado" in visitante.inner_text("#resultadoConfirmacao")
    assert banco.valor("SELECT email_confirmado_em FROM usuarios WHERE id = %s", usuario_id) is not None
    visitante.goto(link)  # clicar de novo não dá erro
    assert "E-mail confirmado" in visitante.inner_text("#resultadoConfirmacao")

    pagina.goto("/Cliente/MinhaConta")
    assert "E-mail confirmado" in pagina.inner_text("#statusEmail")
    assert pagina.locator("input[value='Reenviar e-mail de confirmação']").count() == 0


def test_link_alterado_ou_vencido_nao_confirma(paginas, fabrica, banco, caixa):
    email = f"link.{fabrica.sufixo()}@teste.com"
    pagina = paginas.nova()
    cadastrar(pagina, "Link", email)
    link = caixa.link(caixa.esperar(email, "Bem-vindo")[0], "/conta/confirmar-email")
    for errado in (link[:-6] + "AAAAAA", link.split("?")[0] + "?codigo=", link.split("?")[0]):
        pagina.goto(errado)
        assert "Link inválido ou vencido" in pagina.inner_text("main")
    assert banco.valor("SELECT email_confirmado_em FROM usuarios WHERE email = %s", email) is None


def test_reenviar_confirmacao_e_trocar_de_email(paginas, fabrica, banco, caixa):
    cliente = fabrica.cliente(confirmado=False)
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/MinhaConta")
    enviar(pagina, "input[value='Reenviar e-mail de confirmação']")
    assert f"Enviamos um novo link para {cliente['email']}" in pagina.inner_text(".alert-success")
    enviar(pagina, "input[value='Reenviar e-mail de confirmação']")  # logo em seguida: segura o reenvio
    assert "tente de novo em alguns minutos" in pagina.inner_text(".alert-danger")
    [primeiro] = caixa.esperar(cliente["email"], "Confirme seu e-mail")
    link_antigo = caixa.link(primeiro, "/conta/confirmar-email")

    novo = f"trocado.{fabrica.sufixo()}@teste.com"
    pagina.fill("#Email", novo)
    pagina.fill("#SenhaAtual", cliente["senha"])
    enviar(pagina, "input[value=Salvar]")
    assert f"Enviamos um link para {novo}" in pagina.inner_text(".alert-success")
    [segundo] = caixa.esperar(novo, "Confirme seu e-mail")

    pagina.goto(link_antigo)  # link do e-mail antigo não confirma o novo
    assert "Link inválido ou vencido" in pagina.inner_text("main")
    pagina.goto(caixa.link(segundo, "/conta/confirmar-email"))
    assert novo in pagina.inner_text("#resultadoConfirmacao")
    assert banco.valor("SELECT email_confirmado_em IS NOT NULL FROM usuarios WHERE id = %s", cliente["id"])


def test_admin_ve_o_registro_dos_emails(paginas, fabrica, banco, caixa):
    email = f"registro.{fabrica.sufixo()}@teste.com"
    cadastrar(paginas.nova(), "Registro", email)
    caixa.esperar(email, "Bem-vindo")
    banco.executar("""INSERT INTO emails (para, assunto, tipo, html, texto, criado_em, tentativas, ultimo_erro)
                      VALUES (%s, 'Falhou de propósito', 'PedidoPago', NULL, NULL, now(), 5, 'Servidor recusou')""", email)

    admin = paginas.admin()
    assert admin.locator("nav a", has_text="E-mails").count() == 1
    admin.goto(f"/Admin/Emails?busca={email}")
    linhas = admin.locator("tbody tr")
    assert linhas.count() == 2
    assert "Enviado" in linhas.filter(has_text="Bem-vindo").inner_text()
    assert "Falhou" in linhas.filter(has_text="Falhou de propósito").inner_text()
    assert "Servidor recusou" in linhas.filter(has_text="Falhou de propósito").inner_text()
    admin.goto(f"/Admin/Emails?busca={email}&situacao=falhas")
    assert admin.locator("tbody tr").count() == 1
    admin.goto(f"/Admin/Emails?busca={email}&tipo=BoasVindas")
    assert admin.locator("tbody tr").count() == 1

    # excluir a conta mantém o registro dos e-mails, sem o vínculo
    usuario_id = banco.valor("SELECT id FROM usuarios WHERE email = %s", email)
    admin.goto(f"/Admin/Usuarios/Exclui/{usuario_id}")
    enviar(admin, "input[value=Excluir]")
    assert banco.linhas("SELECT usuario_id FROM emails WHERE para = %s AND tipo = 'BoasVindas'", email) == [(None,)]

    cliente = fabrica.cliente()
    assert paginas.logada(cliente["email"], cliente["senha"]).request.get("/Admin/Emails", max_redirects=0).status == 302


def test_email_que_falha_e_tentado_de_novo(banco, caixa, fabrica):
    """Endereço que o servidor não aceita: a fila guarda o erro e não trava os outros e-mails."""
    ruim = f"sem arroba {fabrica.sufixo()}"
    bom = f"bom.{fabrica.sufixo()}@teste.com"
    for para in (ruim, bom):
        banco.executar("""INSERT INTO emails (para, assunto, tipo, html, texto, criado_em, tentativas)
                          VALUES (%s, 'Teste da fila', 'PedidoPago', '<p>oi</p>', 'oi', now(), 0)""", para)
    caixa.esperar(bom, "Teste da fila")
    limite = time.time() + 10
    while banco.valor("SELECT tentativas FROM emails WHERE para = %s", ruim) < 2 and time.time() < limite:
        time.sleep(0.3)
    tentativas, erro, enviado = banco.linhas("SELECT tentativas, ultimo_erro, enviado_em FROM emails WHERE para = %s", ruim)[0]
    assert tentativas >= 2 and erro and enviado is None


def test_pedido_pago_manda_as_chaves_por_email(paginas, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=1234.5, chaves=3)
    codigos = banco.codigos("produto_id = %s", jogo["produto_id"])
    cliente = fabrica.cliente()
    banco.executar("INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em) VALUES (%s, %s, 2, now())",
                   cliente["id"], jogo["produto_id"])
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/Carrinho")
    enviar(pagina, "input[value='Finalizar compra']")
    pedido = int(pagina.url.rsplit("/", 1)[1])

    enviar(pagina, "button[value=false]")  # recusado: nada de e-mail
    enviar(pagina, "button[value=true]")
    assert "também foram enviadas para o seu e-mail" in pagina.inner_text(".alert-success")

    [mensagem] = caixa.esperar(cliente["email"], f"Pedido #{pedido} aprovado")
    vendidas = banco.codigos("pedido_item_id IN (SELECT id FROM pedido_itens WHERE pedido_id = %s)", pedido)
    assert len(vendidas) == 2
    for codigo in vendidas:
        assert codigo in mensagem["html"] and codigo in mensagem["texto"]
    fora = set(codigos) - set(vendidas)
    assert all(c not in mensagem["html"] for c in fora)  # a chave que não foi vendida não aparece
    assert jogo["titulo"] in mensagem["texto"] and "R$ 2.469,00" in mensagem["texto"]
    assert "Como ativar:" in mensagem["texto"] and "Adicionar um jogo" in mensagem["texto"]  # instruções da Steam
    assert caixa.link(mensagem, f"/Cliente/Pedidos/Detalhes/{pedido}")
    assert len(caixa.de(cliente["email"])) == 1
    assert banco.valor("SELECT html FROM emails WHERE usuario_id = %s AND tipo = 'PedidoPago'", cliente["id"]) is None


def test_email_nao_confirmado_recebe_o_pedido_sem_as_chaves(paginas, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=50, chaves=1)
    cliente = fabrica.cliente(confirmado=False)
    banco.executar("INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em) VALUES (%s, %s, 1, now())",
                   cliente["id"], jogo["produto_id"])
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/Carrinho")
    enviar(pagina, "input[value='Finalizar compra']")
    pedido = int(pagina.url.rsplit("/", 1)[1])
    enviar(pagina, "button[value=true]")
    [mensagem] = caixa.esperar(cliente["email"], f"Pedido #{pedido} aprovado")
    [codigo] = banco.codigos("produto_id = %s", jogo["produto_id"])
    assert codigo not in mensagem["html"] and codigo not in mensagem["texto"]
    assert "ainda não foi confirmado" in mensagem["texto"]
    assert codigo in pagina.inner_text("main")  # na loja a chave aparece normalmente
