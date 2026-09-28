"""Sessão: logout por POST, selo de segurança derrubando sessões antigas, cookies e antiforgery."""
from conftest import caminho, entrar, enviar


def logado(pagina) -> bool:
    return pagina.request.get("/Cliente/Pedidos", max_redirects=0).status == 200


def test_logout_e_por_post(paginas, fabrica):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/conta/logout")  # um link (ou uma imagem em outro site) não desloga
    assert logado(pagina) and "Sair da conta?" in pagina.inner_text("main")
    assert pagina.request.post("/conta/logout", max_redirects=0).status == 400  # sem token
    assert logado(pagina)
    pagina.goto("/")
    enviar(pagina, "#botaoSair")
    assert caminho(pagina) == "/" and not logado(pagina)
    assert pagina.locator("nav a", has_text="Entrar").count() == 1


def test_trocar_a_senha_derruba_as_outras_sessoes(paginas, fabrica):
    cliente = fabrica.cliente()
    celular = paginas.logada(cliente["email"], cliente["senha"])
    computador = paginas.logada(cliente["email"], cliente["senha"])
    computador.goto("/Cliente/MinhaConta")
    computador.fill("#SenhaAtual", cliente["senha"])
    computador.fill("#NovaSenha", "outra-senha-99")
    computador.fill("#ConfirmarNovaSenha", "outra-senha-99")
    enviar(computador, "input[value=Salvar]")
    assert "Seus dados foram atualizados." in computador.inner_text("main")
    assert logado(computador)       # quem trocou continua dentro
    assert not logado(celular)      # o outro aparelho sai


def test_sair_de_todos_os_aparelhos(paginas, fabrica):
    cliente = fabrica.cliente()
    outro = paginas.logada(cliente["email"], cliente["senha"])
    este = paginas.logada(cliente["email"], cliente["senha"])
    este.goto("/Cliente/MinhaConta")
    enviar(este, "input[value='Sair de todos os outros aparelhos']")
    assert "desconectada dos outros aparelhos" in este.inner_text(".alert-success")
    assert logado(este) and not logado(outro)


def test_admin_rebaixado_ou_usuario_excluido_perde_o_acesso(paginas, fabrica, banco):
    segundo = fabrica.cliente("Outro Admin")
    banco.executar("UPDATE usuarios SET perfil = 1 WHERE id = %s", segundo["id"])
    pagina = paginas.logada(segundo["email"], segundo["senha"])
    assert pagina.request.get("/Admin/Jogo", max_redirects=0).status == 200

    admin = paginas.admin()
    admin.goto(f"/Admin/Usuarios/Altera/{segundo['id']}")
    admin.select_option("#Perfil", "Cliente")
    enviar(admin, "input[value=Salvar]")
    assert pagina.request.get("/Admin/Jogo", max_redirects=0).status == 302  # a sessão caiu

    entrar(pagina, segundo["email"], segundo["senha"])
    assert pagina.request.get("/Admin/Jogo", max_redirects=0).status == 302  # agora é cliente
    assert logado(pagina)
    admin.goto(f"/Admin/Usuarios/Exclui/{segundo['id']}")
    enviar(admin, "input[value=Excluir]")
    assert not logado(pagina)


def test_admin_troca_o_email_de_um_cliente(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    admin = paginas.admin()
    admin.goto(f"/Admin/Usuarios/Altera/{cliente['id']}")
    novo = f"novo.{fabrica.sufixo()}@teste.com"
    admin.fill("#Email", novo)
    enviar(admin, "input[value=Salvar]")
    assert banco.valor("SELECT email_confirmado_em FROM usuarios WHERE id = %s", cliente["id"]) is None
    assert not logado(pagina)


def test_cookie_de_sessao_protegido(paginas, fabrica):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    [sessao] = [c for c in pagina.context.cookies() if c["name"] == "clouud.sessao"]
    assert sessao["httpOnly"] and sessao["sameSite"] == "Lax"
    assert pagina.evaluate("document.cookie").find("clouud.sessao") == -1  # JavaScript não enxerga


def test_cookie_antigo_sem_selo_nao_vale(paginas, fabrica, banco):
    """Quem estava logado antes desta versão (cookie sem o selo) precisa entrar de novo."""
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    banco.executar("UPDATE usuarios SET selo_seguranca = 'trocado-direto-no-banco' WHERE id = %s", cliente["id"])
    assert not logado(pagina)
