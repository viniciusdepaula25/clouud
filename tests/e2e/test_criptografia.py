"""Chaves de ativação cifradas no banco (AES-GCM) e o que depende disso."""
import pytest

from conftest import COFRE, SAIDA, Aplicacao, ambiente_da_aplicacao, enviar


def importar(admin, produto_id, texto):
    admin.goto(f"/Admin/Estoque/Importar/{produto_id}")
    admin.fill("#Codigos", texto)
    enviar(admin, "input[value=Importar]")
    return admin.inner_text("main")


def test_codigo_fica_cifrado_no_banco(paginas, fabrica, banco):
    jogo = fabrica.jogo()
    codigo = f"SEGREDO-{fabrica.sufixo().upper()}"
    admin = paginas.admin()
    assert "1 chave importada." in importar(admin, jogo["produto_id"], codigo)

    cifrado, hash_ = banco.linhas("SELECT codigo_cifrado, codigo_hash FROM chaves WHERE produto_id = %s", jogo["produto_id"])[0]
    assert cifrado.startswith("v1:") and codigo not in cifrado
    assert COFRE.decifrar(cifrado) == codigo and hash_ == COFRE.hash(codigo)
    # nenhuma coluna da tabela guarda o código aberto
    assert banco.valor("SELECT count(*) FROM chaves WHERE row_to_json(chaves)::text LIKE %s", f"%{codigo}%") == 0

    # repetida em outro produto: achada pelo hash, sem decifrar o estoque
    outro = fabrica.jogo()
    assert f"Já cadastradas: {codigo}" in importar(admin, outro["produto_id"], codigo)


def test_admin_so_ve_codigos_quando_pede_e_nunca_os_vendidos(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=2)
    vendida, livre = banco.codigos("produto_id = %s", jogo["produto_id"])
    cliente = fabrica.cliente()
    pedido = fabrica.pedido_pago(cliente["id"], jogo["produto_id"])
    item = banco.valor("SELECT id FROM pedido_itens WHERE pedido_id = %s", pedido)
    banco.executar("""UPDATE chaves SET status = 'Vendida', pedido_item_id = %s
                      WHERE id = (SELECT min(id) FROM chaves WHERE produto_id = %s)""", item, jogo["produto_id"])

    admin = paginas.admin()
    resposta = admin.goto(f"/Admin/Estoque/Chaves/{jogo['produto_id']}")
    assert "no-store" in resposta.headers.get("cache-control", "")
    html = admin.content()
    assert vendida not in html and livre not in html
    enviar(admin, "#mostrarCodigos")
    html = admin.content()
    assert livre in html and vendida not in html
    assert "abriu os códigos das chaves" in (SAIDA / "app.log").read_text()


def test_paginas_com_chaves_nao_ficam_no_cache(paginas, fabrica):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    assert "no-store" in pagina.goto("/Cliente/MinhasChaves").headers.get("cache-control", "")


@pytest.mark.lento
def test_chaves_antigas_em_texto_puro_sao_cifradas_ao_iniciar(app, fabrica, banco):
    jogo = fabrica.jogo()
    codigo = f"ANTIGA-{fabrica.sufixo().upper()}"
    chave_id = banco.valor("""INSERT INTO chaves (produto_id, codigo_cifrado, status, adicionada_em)
                              VALUES (%s, %s, 'Disponivel', now()) RETURNING id""", jogo["produto_id"], codigo)
    segunda = Aplicacao("http://127.0.0.1:5098", ambiente_da_aplicacao("http://127.0.0.1:5098"), SAIDA / "app-conversao.log")
    try:
        assert segunda.esperar()
        cifrado, hash_ = banco.linhas("SELECT codigo_cifrado, codigo_hash FROM chaves WHERE id = %s", chave_id)[0]
        assert cifrado.startswith("v1:") and COFRE.decifrar(cifrado) == codigo and hash_ == COFRE.hash(codigo)
        assert "chave(s) de ativação em texto puro foram cifradas" in segunda.texto_do_log()
    finally:
        segunda.parar()


@pytest.mark.lento
@pytest.mark.parametrize("chave, mensagem", [
    ("", "Falta a chave de criptografia"),
    ("nao-e-base64!", "não está em Base64"),
    ("c2hvcnQ=", "precisa ter 32 bytes"),
])
def test_sem_chave_de_criptografia_valida_a_aplicacao_nao_sobe(app, chave, mensagem):
    url = "http://127.0.0.1:5099"
    outra = Aplicacao(url, ambiente_da_aplicacao(url, Seguranca__ChaveCriptografia=chave), SAIDA / "app-sem-chave.log")
    try:
        assert outra.esperar() is False  # o processo termina
        assert mensagem in outra.texto_do_log()
    finally:
        outra.parar()
