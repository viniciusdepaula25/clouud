"""Conta do cliente: cadastro com entrada direta, login, Minha conta e foto de perfil."""
import re

from conftest import ARQUIVOS, UPLOADS, caminho, entrar, enviar, token


def cadastrar(pagina, nome, email, senha="segredo1"):
    pagina.goto("/conta/cadastro")
    pagina.fill("#Nome", nome)
    pagina.fill("#Email", email)
    pagina.fill("#Senha", senha)
    if pagina.locator("#ConfirmarSenha").count():
        pagina.fill("#ConfirmarSenha", senha)
    with pagina.expect_navigation():
        pagina.locator("form input[type=submit], form button[type=submit]").last.click()


def test_cadastro_entra_direto_e_email_repetido_e_recusado(paginas, fabrica, banco):
    email = f"carla.{fabrica.sufixo()}@teste.com"
    pagina = paginas.nova()
    cadastrar(pagina, "Carla", f"  {email.upper()} ")
    assert caminho(pagina) == "/", pagina.url  # entra direto na loja
    assert "Conta criada. Bem-vindo(a), Carla!" in pagina.inner_text(".alert-success")
    assert "Carla" in pagina.inner_text("nav")
    perfil, senha = banco.linhas("SELECT perfil, senha FROM usuarios WHERE email = %s", email)[0]
    assert perfil == 0 and senha != "segredo1"  # cliente, e-mail em minúsculo, senha com hash
    pagina.reload()
    assert pagina.locator(".alert-success").count() == 0  # a mensagem aparece uma vez só

    outra = paginas.nova()
    cadastrar(outra, "Outra", email)
    assert "Já existe uma conta com este e-mail" in outra.inner_text("main")
    assert outra.request.get("/Cliente/Pedidos", max_redirects=0).status == 302  # não ficou logada

    terceira = paginas.nova()
    entrar(terceira, email, "segredo1")
    assert caminho(terceira) == "/" and "Sair" in terceira.inner_text("nav")


def test_login_errado_e_senha_antiga_convertida_para_hash(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    entrar(pagina, cliente["email"], "errada")
    assert "/conta/login" in pagina.url.lower()
    entrar(pagina, cliente["email"], cliente["senha"])
    assert caminho(pagina) == "/" and "Sair" in pagina.inner_text("nav")
    assert banco.valor("SELECT senha FROM usuarios WHERE id = %s", cliente["id"]) != cliente["senha"]


def test_return_url_externo_e_ignorado(paginas, fabrica):
    cliente = fabrica.cliente()
    pagina = paginas.nova()
    entrar(pagina, cliente["email"], cliente["senha"], url="/conta/login?returnUrl=https://exemplo.com/roubo")
    assert pagina.url.startswith(paginas.url) and caminho(pagina) == "/"


def foto_do_banco(banco, usuario_id):
    return banco.valor("SELECT foto FROM usuarios WHERE id = %s", usuario_id)


def enviar_foto(pagina, arquivo):
    pagina.goto("/Cliente/MinhaConta")
    pagina.set_input_files("#arquivoFoto", str(arquivo))
    enviar(pagina, "input[value='Enviar foto']")


def test_foto_de_perfil(paginas, fabrica, banco, arquivo_grande):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    avatar = pagina.locator("nav img[alt='Minha conta']")
    assert avatar.get_attribute("src") == "/img/perfil.jpg"

    enviar_foto(pagina, ARQUIVOS / "imagem.png")
    f1 = foto_do_banco(banco, cliente["id"])
    assert "Foto atualizada." in pagina.inner_text("main") and re.fullmatch(r"perfis/[a-z0-9]+\.png", f1)
    assert avatar.get_attribute("src") == f"/uploads/{f1}" and pagina.locator("#fotoPerfil").get_attribute("src") == f"/uploads/{f1}"
    assert pagina.request.get(f"/uploads/{f1}").status == 200

    enviar_foto(pagina, ARQUIVOS / "imagem.jpg")
    f2 = foto_do_banco(banco, cliente["id"])
    assert f2.endswith(".jpg") and not (UPLOADS / f1).exists() and (UPLOADS / f2).exists()  # troca apaga a antiga

    for arquivo in ("texto-renomeado.png", "script.svg"):
        enviar_foto(pagina, ARQUIVOS / arquivo)
        assert "Formato não aceito" in pagina.inner_text(".alert-danger") and foto_do_banco(banco, cliente["id"]) == f2

    # mais de 2 MB: o navegador avisa e limpa o campo; o servidor também recusa se o aviso for contornado
    pagina.goto("/Cliente/MinhaConta")
    pagina.set_input_files("#arquivoFoto", str(arquivo_grande))
    assert "no máximo 2 MB" in pagina.inner_text("#erroFoto") and pagina.input_value("#arquivoFoto") == ""
    pagina.request.post("/Cliente/MinhaConta/Foto", max_redirects=0, multipart={
        "__RequestVerificationToken": token(pagina),
        "arquivo": {"name": "grande.png", "mimeType": "image/png", "buffer": arquivo_grande.read_bytes()}})
    pagina.goto("/Cliente/MinhaConta")
    assert "no máximo 2 MB" in pagina.inner_text(".alert-danger") and foto_do_banco(banco, cliente["id"]) == f2

    pagina.set_input_files("#arquivoFoto", str(ARQUIVOS / "imagem.png"))
    assert pagina.locator("#fotoPerfil").get_attribute("src").startswith("blob:")  # prévia antes de enviar

    pagina.goto("/Cliente/MinhaConta")
    pagina.fill("#Nome", "Nome Novo")
    enviar(pagina, "input[value=Salvar]")
    assert "Seus dados foram atualizados." in pagina.inner_text("main") and "Nome Novo" in pagina.inner_text("nav")
    assert foto_do_banco(banco, cliente["id"]) == f2  # salvar os dados mantém a foto

    enviar(pagina, "input[value='Remover foto']")
    assert "Foto removida." in pagina.inner_text("main") and foto_do_banco(banco, cliente["id"]) is None
    assert not (UPLOADS / f2).exists() and avatar.get_attribute("src") == "/img/perfil.jpg"

    assert paginas.nova().request.post("/Cliente/MinhaConta/Foto", max_redirects=0).status in (302, 400)  # visitante


def test_admin_ve_a_foto_e_excluir_usuario_apaga_o_arquivo(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    enviar_foto(pagina, ARQUIVOS / "imagem.png")
    foto = foto_do_banco(banco, cliente["id"])

    admin = paginas.admin()
    admin.goto("/Admin/Usuarios")
    assert admin.locator(f"tbody img[src='/uploads/{foto}']").count() == 1
    admin.goto(f"/Admin/Usuarios/Exclui/{cliente['id']}")
    assert admin.locator(f"img[src='/uploads/{foto}']").count() == 1
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main") and not (UPLOADS / foto).exists()
