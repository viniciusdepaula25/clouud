"""Imagens dos jogos: galeria (admin e página do jogo) e validação da capa."""
import re

import pytest

from conftest import ARQUIVOS, UPLOADS, enviar, token

PNG = (ARQUIVOS / "imagem.png").read_bytes()
JPG = (ARQUIVOS / "imagem.jpg").read_bytes()


def imagens(n, inicio=1):
    """n imagens válidas com nomes diferentes (o conteúdo pode repetir)."""
    return [{"name": f"g{i}.png", "mimeType": "image/png", "buffer": PNG} for i in range(inicio, inicio + n)]


def enviar_galeria(admin, arquivos):
    admin.set_input_files("#arquivos", arquivos)
    enviar(admin, "input[value=Enviar]")


def ids_em_ordem(banco, jogo_id):
    return [linha[0] for linha in banco.linhas("SELECT id FROM jogo_imagens WHERE jogo_id = %s ORDER BY ordem, id", jogo_id)]


@pytest.fixture
def admin(paginas):
    return paginas.admin()


def test_galeria_envio_validacoes_e_limite(admin, fabrica, banco, arquivo_grande):
    jogo = fabrica.jogo()
    admin.goto(f"/Admin/Jogo?busca={jogo['titulo']}")
    assert "Galeria (0)" in admin.locator("tbody tr", has_text=jogo["titulo"]).inner_text()
    admin.goto(f"/Admin/Galeria/Index/{jogo['jogo_id']}")
    assert "Nenhuma imagem" in admin.inner_text("main")

    enviar_galeria(admin, [str(ARQUIVOS / "imagem.jpg"), str(ARQUIVOS / "imagem.png"), str(ARQUIVOS / "imagem.webp"),
                           str(ARQUIVOS / "texto-renomeado.png"), str(ARQUIVOS / "script.svg")])
    texto = admin.inner_text("main")
    assert "3 imagens adicionadas." in texto
    assert "texto-renomeado.png: Formato não aceito" in texto and "script.svg: Formato não aceito" in texto
    arquivos = [linha[0] for linha in banco.linhas("SELECT arquivo FROM jogo_imagens WHERE jogo_id = %s", jogo["jogo_id"])]
    assert admin.locator(".imagem-galeria").count() == 3 and len(arquivos) == 3
    assert all(re.fullmatch(r"galeria/[a-z0-9]+\.(jpg|png|webp)", a) and (UPLOADS / a).exists() for a in arquivos), arquivos

    enviar_galeria(admin, [str(arquivo_grande)])
    assert "no máximo 5 MB" in admin.inner_text(".alert-danger") and admin.locator(".imagem-galeria").count() == 3

    enviar_galeria(admin, imagens(10))
    texto = admin.inner_text("main")
    assert "9 imagens adicionadas." in texto and "máximo de 12" in texto
    assert admin.locator(".imagem-galeria").count() == 12 and admin.locator("#arquivos").is_disabled()


def test_galeria_legenda_ordem_exclusao_e_pagina_do_jogo(admin, fabrica, banco, paginas):
    jogo = fabrica.jogo(chaves=1)
    admin.goto(f"/Admin/Galeria/Index/{jogo['jogo_id']}")
    enviar_galeria(admin, imagens(5))
    primeira = ids_em_ordem(banco, jogo["jogo_id"])[0]

    card = admin.locator(".imagem-galeria").first
    card.locator("input[name=legenda]").fill("Tela inicial <b>negrito</b>")
    with admin.expect_navigation():
        card.locator("input[value=Salvar]").click()
    assert banco.valor("SELECT legenda FROM jogo_imagens WHERE id = %s", primeira) == "Tela inicial <b>negrito</b>"

    assert admin.locator(".imagem-galeria").first.locator("button[aria-label='Mover para a esquerda']").is_disabled()
    with admin.expect_navigation():
        admin.locator(".imagem-galeria").first.locator("button[aria-label='Mover para a direita']").click()
    assert ids_em_ordem(banco, jogo["jogo_id"])[1] == primeira
    with admin.expect_navigation():
        admin.locator(".imagem-galeria").nth(1).locator("button[aria-label='Mover para a esquerda']").click()
    assert ids_em_ordem(banco, jogo["jogo_id"])[0] == primeira

    ultima = banco.valor("SELECT arquivo FROM jogo_imagens WHERE jogo_id = %s ORDER BY ordem DESC, id DESC LIMIT 1", jogo["jogo_id"])
    with admin.expect_navigation():
        admin.locator(".imagem-galeria").last.locator("input[value=Excluir]").click()
    assert admin.locator(".imagem-galeria").count() == 4 and not (UPLOADS / ultima).exists()

    visitante = paginas.nova()
    visitante.goto(f"/jogo/{jogo['slug']}")
    itens = visitante.locator("#galeria .carousel-item")
    assert itens.count() == 4 and visitante.locator(".miniatura").count() == 4
    assert visitante.locator("#galeria .carousel-item.active img").get_attribute("alt") == "Tela inicial <b>negrito</b>"
    for i in range(4):
        assert visitante.request.get(itens.nth(i).locator("img").get_attribute("src")).status == 200
    visitante.locator(".miniatura").nth(2).click()
    visitante.wait_for_timeout(900)  # animação do carrossel
    assert "active" in itens.nth(2).get_attribute("class") and "ativa" in visitante.locator(".miniatura").nth(2).get_attribute("class")
    visitante.click(".carousel-control-next")
    visitante.wait_for_timeout(900)
    assert "active" in itens.nth(3).get_attribute("class")
    assert visitante.locator(".miniatura").nth(3).get_attribute("aria-current") == "true"

    sem_galeria = fabrica.jogo(chaves=1)
    visitante.goto(f"/jogo/{sem_galeria['slug']}")
    assert visitante.locator("#galeria").count() == 0


def test_excluir_jogo_apaga_a_galeria_e_cliente_nao_acessa(admin, fabrica, banco, paginas):
    jogo_id = banco.valor("INSERT INTO jogos (titulo, slug, ativo, destaque) VALUES (%s, %s, true, false) RETURNING id",
                          "Com galeria", f"com-galeria-{fabrica.sufixo()}")
    admin.goto(f"/Admin/Galeria/Index/{jogo_id}")
    enviar_galeria(admin, imagens(2))
    arquivos = [linha[0] for linha in banco.linhas("SELECT arquivo FROM jogo_imagens WHERE jogo_id = %s", jogo_id)]
    admin.goto(f"/Admin/Jogo/Exclui/{jogo_id}")
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main")
    assert banco.valor("SELECT count(*) FROM jogo_imagens WHERE jogo_id = %s", jogo_id) == 0
    assert not any((UPLOADS / a).exists() for a in arquivos)

    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    assert pagina.request.get(f"/Admin/Galeria/Index/{jogo_id}", max_redirects=0).status == 302


# ---------------------------------------------------------------- capa

def incluir_jogo(admin, titulo, arquivo=None):
    admin.goto("/Admin/Jogo/Inclui")
    admin.fill("#Titulo", titulo)
    if arquivo:
        admin.set_input_files("#txtArquivo", str(arquivo))
    enviar(admin, "input[value=Cadastrar]")


def test_capa_invalida_nao_cria_o_jogo(admin, fabrica, banco, arquivo_grande):
    titulo = f"Capa Falsa {fabrica.sufixo()}"
    capas_antes = set((UPLOADS / "capas").glob("*")) if (UPLOADS / "capas").exists() else set()
    for arquivo in ("texto-renomeado.png", "script.svg"):
        incluir_jogo(admin, titulo, ARQUIVOS / arquivo)
        assert "Capa: Formato não aceito" in admin.inner_text("#erroCapa")
        assert admin.input_value("#Titulo") == titulo  # o formulário volta preenchido
    assert banco.valor("SELECT count(*) FROM jogos WHERE titulo = %s", titulo) == 0
    assert (set((UPLOADS / "capas").glob("*")) if (UPLOADS / "capas").exists() else set()) == capas_antes

    admin.goto("/Admin/Jogo/Inclui")
    admin.set_input_files("#txtArquivo", str(arquivo_grande))
    assert "no máximo 5 MB" in admin.inner_text("#erroCapa") and admin.input_value("#txtArquivo") == ""
    resposta = admin.request.post("/Admin/Jogo/Inclui", max_redirects=0, multipart={
        "__RequestVerificationToken": token(admin), "Titulo": titulo, "Ativo": "true",
        "arquivo": {"name": "grande.png", "mimeType": "image/png", "buffer": arquivo_grande.read_bytes()}})
    assert resposta.status == 200 and "no máximo 5 MB" in resposta.text()
    assert banco.valor("SELECT count(*) FROM jogos WHERE titulo = %s", titulo) == 0

    admin.goto("/Admin/Jogo/Inclui")
    admin.set_input_files("#txtArquivo", str(ARQUIVOS / "imagem.jpg"))
    with admin.expect_navigation():
        admin.evaluate("const f = document.querySelector('main form'); f.noValidate = true; f.submit()")
    assert "Título obrigatório" in admin.inner_text("main")
    assert (set((UPLOADS / "capas").glob("*")) if (UPLOADS / "capas").exists() else set()) == capas_antes


def test_troca_de_capa(admin, fabrica, banco):
    titulo = f"Capa Boa {fabrica.sufixo()}"
    incluir_jogo(admin, titulo, ARQUIVOS / "imagem.png")
    jogo_id, capa = banco.linhas("SELECT id, capa FROM jogos WHERE titulo = %s", titulo)[0]
    assert re.fullmatch(r"capas/[a-z0-9]+\.png", capa) and (UPLOADS / capa).exists()
    assert admin.request.get(f"/uploads/{capa}").status == 200

    admin.goto(f"/Admin/Jogo/Altera/{jogo_id}")
    admin.set_input_files("#txtArquivo", str(ARQUIVOS / "texto-renomeado.png"))
    enviar(admin, "input[value=Salvar]")
    assert "Formato não aceito" in admin.inner_text("#erroCapa") and admin.locator("img[alt='Capa atual']").count() == 1
    assert banco.valor("SELECT capa FROM jogos WHERE id = %s", jogo_id) == capa and (UPLOADS / capa).exists()

    admin.set_input_files("#txtArquivo", str(ARQUIVOS / "imagem.jpg"))
    enviar(admin, "input[value=Salvar]")
    nova = banco.valor("SELECT capa FROM jogos WHERE id = %s", jogo_id)
    assert nova.startswith("capas/") and nova.endswith(".jpg") and (UPLOADS / nova).exists() and not (UPLOADS / capa).exists()

    # capa do formato antigo (direto em uploads/) continua aparecendo e é apagada ao trocar
    legado = f"legado_{fabrica.sufixo()}.png"
    (UPLOADS / legado).write_bytes(PNG)
    banco.executar("UPDATE jogos SET capa = %s WHERE id = %s", legado, jogo_id)
    admin.goto(f"/Admin/Jogo/Altera/{jogo_id}")
    assert admin.locator("img[alt='Capa atual']").get_attribute("src") == f"/uploads/{legado}"
    admin.set_input_files("#txtArquivo", str(ARQUIVOS / "imagem.png"))
    enviar(admin, "input[value=Salvar]")
    final = banco.valor("SELECT capa FROM jogos WHERE id = %s", jogo_id)
    assert final.startswith("capas/") and not (UPLOADS / legado).exists()

    admin.goto(f"/Admin/Jogo/Exclui/{jogo_id}")
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main") and not (UPLOADS / final).exists()
