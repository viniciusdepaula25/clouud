"""Avisos da lista de desejos por e-mail: volta ao estoque e promoção."""
import time

import pytest

from conftest import enviar


def confirmar_email(banco, usuario):
    banco.executar("UPDATE usuarios SET email_confirmado_em = now() WHERE id = %s", usuario["id"])


def desejar(banco, usuario, jogo):
    banco.executar("INSERT INTO lista_desejos (usuario_id, jogo_id, adicionado_em) VALUES (%s, %s, now())",
                   usuario["id"], jogo["jogo_id"])


def esperar_conferencia(banco, usuario, jogo, segundos=10):
    """O serviço de avisos passa a cada segundo nos testes; a primeira passada só guarda a situação."""
    limite = time.time() + segundos
    while not banco.valor("SELECT aviso_conferido_em IS NOT NULL FROM lista_desejos WHERE usuario_id = %s AND jogo_id = %s",
                          usuario["id"], jogo["jogo_id"]):
        assert time.time() < limite, "o serviço de avisos não conferiu a lista"
        time.sleep(0.3)
    time.sleep(1.5)  # mais uma volta completa, para garantir que nada foi enviado por engano


def importar_chaves(admin, produto_id, codigos):
    admin.goto(f"/Admin/Estoque/Importar/{produto_id}")
    admin.fill("textarea", "\n".join(codigos))
    enviar(admin, "form input[type=submit] >> nth=-1")


def mudar_promocao(admin, produto_id, preco):
    admin.goto(f"/Admin/Produto/Altera/{produto_id}")
    admin.fill("#PrecoPromocional", preco)
    enviar(admin, "input[value=Salvar]")


@pytest.fixture
def admin(paginas):
    return paginas.admin()


def test_volta_ao_estoque_e_promocao(admin, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=200, chaves=0)
    cliente = fabrica.cliente()
    confirmar_email(banco, cliente)
    desejar(banco, cliente, jogo)
    esperar_conferencia(banco, cliente, jogo)
    assert caixa.de(cliente["email"]) == []  # entrar na lista não gera e-mail

    importar_chaves(admin, jogo["produto_id"], [f"VOLTA{fabrica.sufixo()}"])
    [voltou] = caixa.esperar(cliente["email"], "voltou ao estoque")
    assert jogo["titulo"] in voltou["assunto"] and "R$ 200,00" in voltou["texto"]
    assert caixa.link(voltou, f"/jogo/{jogo['slug']}")

    mudar_promocao(admin, jogo["produto_id"], "150,00")
    [promo] = caixa.esperar(cliente["email"], "entrou em promoção")
    assert "R$ 150,00" in promo["texto"] and "-25%" in promo["texto"]

    mudar_promocao(admin, jogo["produto_id"], "120")    # ficou mais barata: avisa de novo
    caixa.esperar(cliente["email"], "entrou em promoção", quantidade=2)
    mudar_promocao(admin, jogo["produto_id"], "130")    # ficou mais cara: não avisa
    time.sleep(3)
    assert len(caixa.de(cliente["email"])) == 3


def test_esgotado_que_volta_em_promocao_manda_um_aviso_so(admin, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=100, promocional=60, chaves=0)
    cliente = fabrica.cliente()
    confirmar_email(banco, cliente)
    desejar(banco, cliente, jogo)
    esperar_conferencia(banco, cliente, jogo)
    importar_chaves(admin, jogo["produto_id"], [f"PROMO{fabrica.sufixo()}"])
    [aviso] = caixa.esperar(cliente["email"], "voltou ao estoque e está em promoção")
    assert "R$ 60,00" in aviso["texto"]
    time.sleep(3)
    assert len(caixa.de(cliente["email"])) == 1


def test_pedido_cancelado_que_devolve_chave_tambem_avisa(paginas, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=80, chaves=1)
    comprador, interessado = fabrica.cliente(), fabrica.cliente()
    confirmar_email(banco, interessado)
    banco.executar("INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em) VALUES (%s, %s, 1, now())",
                   comprador["id"], jogo["produto_id"])
    pagina = paginas.logada(comprador["email"], comprador["senha"])
    pagina.goto("/Cliente/Carrinho")
    enviar(pagina, "input[value='Finalizar compra']")  # a única chave fica reservada: esgotado
    desejar(banco, interessado, jogo)
    esperar_conferencia(banco, interessado, jogo)
    enviar(pagina, "input[value='Cancelar pedido']")    # a chave volta
    caixa.esperar(interessado["email"], "voltou ao estoque")


def test_sem_confirmacao_ou_com_avisos_desligados_nao_recebe(admin, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=100, chaves=0)
    sem_confirmar, desligado, confirmado = fabrica.cliente(), fabrica.cliente(), fabrica.cliente()
    confirmar_email(banco, desligado)
    confirmar_email(banco, confirmado)
    banco.executar("UPDATE usuarios SET receber_avisos = false WHERE id = %s", desligado["id"])
    for cliente in (sem_confirmar, desligado, confirmado):
        desejar(banco, cliente, jogo)
        esperar_conferencia(banco, cliente, jogo, segundos=10)
    importar_chaves(admin, jogo["produto_id"], [f"SO{fabrica.sufixo()}"])
    caixa.esperar(confirmado["email"], "voltou ao estoque")
    time.sleep(2)
    assert caixa.de(sem_confirmar["email"]) == [] and caixa.de(desligado["email"]) == []
    # a situação foi atualizada para todos: confirmar depois não dispara um aviso atrasado
    confirmar_email(banco, sem_confirmar)
    time.sleep(3)
    assert caixa.de(sem_confirmar["email"]) == []


def test_parar_de_receber_pelo_link_e_voltar_em_minha_conta(admin, paginas, fabrica, banco, caixa):
    jogo = fabrica.jogo(preco=100, chaves=0)
    cliente = fabrica.cliente()
    confirmar_email(banco, cliente)
    desejar(banco, cliente, jogo)
    esperar_conferencia(banco, cliente, jogo)
    importar_chaves(admin, jogo["produto_id"], [f"SAIR{fabrica.sufixo()}"])
    [aviso] = caixa.esperar(cliente["email"], "voltou ao estoque")
    link = caixa.link(aviso, "/conta/parar-avisos")
    assert "Parar de receber" in aviso["texto"]

    visitante = paginas.nova()
    visitante.goto(link)
    assert cliente["email"] in visitante.inner_text("#pararAvisos")
    assert banco.valor("SELECT receber_avisos FROM usuarios WHERE id = %s", cliente["id"])  # só abrir não desliga
    enviar(visitante, "input[value='Parar de receber']")
    assert "Pronto" in visitante.inner_text("#pararAvisos")
    assert not banco.valor("SELECT receber_avisos FROM usuarios WHERE id = %s", cliente["id"])
    visitante.goto(link.split("?")[0] + "?codigo=invalido")
    assert "Link inválido" in visitante.inner_text("#pararAvisos")

    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/MinhaConta")
    assert not pagina.is_checked("#ReceberAvisos")
    pagina.check("#ReceberAvisos")
    enviar(pagina, "input[value=Salvar]")
    assert banco.valor("SELECT receber_avisos FROM usuarios WHERE id = %s", cliente["id"])
