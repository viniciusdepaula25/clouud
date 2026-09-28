"""Reforços de segurança nas regras da loja: pagamento simulado, pedidos abertos, overposting, avisos por e-mail."""
import re

import pytest
import requests

from conftest import SAIDA, Aplicacao, ambiente_da_aplicacao, enviar, token


def carrinho_com(banco, cliente, produto_id):
    banco.executar("DELETE FROM carrinho_itens WHERE usuario_id = %s", cliente["id"])
    banco.executar("INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em) VALUES (%s, %s, 1, now())",
                   cliente["id"], produto_id)


def test_no_maximo_dois_pedidos_esperando_pagamento(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=5)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    for _ in range(2):
        carrinho_com(banco, cliente, jogo["produto_id"])
        pagina.goto("/Cliente/Carrinho")
        enviar(pagina, "input[value='Finalizar compra']")
        assert "/Cliente/Pedidos/Pagar/" in pagina.url
    carrinho_com(banco, cliente, jogo["produto_id"])
    pagina.goto("/Cliente/Carrinho")
    enviar(pagina, "input[value='Finalizar compra']")
    assert "já tem 2 pedido(s) esperando pagamento" in pagina.inner_text(".alert-danger")
    assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Reservada'", jogo["produto_id"]) == 2


def test_forma_de_pagamento_inventada_e_recusada(paginas, fabrica, banco):
    jogo = fabrica.jogo(chaves=1)
    cliente = fabrica.cliente()
    carrinho_com(banco, cliente, jogo["produto_id"])
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/Carrinho")
    enviar(pagina, "input[value='Finalizar compra']")
    pedido = pagina.url.rsplit("/", 1)[1]
    pagina.request.post(f"/Cliente/Pedidos/Pagar/{pedido}", form={
        "metodo": "99", "aprovar": "true", "__RequestVerificationToken": token(pagina)})
    assert banco.valor("SELECT status FROM pedidos WHERE id = %s", int(pedido)) == "AguardandoPagamento"
    assert banco.valor("SELECT count(*) FROM pagamentos WHERE pedido_id = %s", int(pedido)) == 0


def test_cupom_forjado_nao_cria_pedidos_nem_escolhe_id(paginas, fabrica, banco):
    cliente = fabrica.cliente()
    admin = paginas.admin()
    admin.goto("/Admin/Cupons/Inclui")
    codigo = f"FORJADO{fabrica.sufixo().upper()}"
    admin.request.post("/Admin/Cupons/Inclui", form={
        "__RequestVerificationToken": token(admin), "Codigo": codigo, "Tipo": "Percentual", "Valor": "10",
        "LimitePorCliente": "1", "Ativo": "true", "Id": "987654", "CriadoEm": "2000-01-01",
        "Pedidos[0].UsuarioId": str(cliente["id"]), "Pedidos[0].Status": "Pago",
        "Pedidos[0].Subtotal": "0", "Pedidos[0].Total": "0"})
    cupom_id, criado = banco.linhas("SELECT id, criado_em::date::text FROM cupons WHERE codigo = %s", codigo)[0]
    assert cupom_id != 987654 and criado != "2000-01-01"
    assert banco.valor("SELECT count(*) FROM pedidos WHERE usuario_id = %s", cliente["id"]) == 0


def test_avisos_de_troca_de_email_e_de_senha(paginas, fabrica, caixa):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    pagina.goto("/Cliente/MinhaConta")
    pagina.fill("#SenhaAtual", cliente["senha"])
    pagina.fill("#NovaSenha", "nova-senha-forte-1")
    pagina.fill("#ConfirmarNovaSenha", "nova-senha-forte-1")
    enviar(pagina, "input[value=Salvar]")
    [senha] = caixa.esperar(cliente["email"], "senha da CLOUUD foi alterada")
    assert "em \"Minha conta\"" in senha["texto"]

    novo = f"trocou.{fabrica.sufixo()}@exemplo.com"
    pagina.goto("/Cliente/MinhaConta")
    pagina.fill("#Email", novo)
    pagina.fill("#SenhaAtual", "nova-senha-forte-1")
    enviar(pagina, "input[value=Salvar]")
    [aviso] = caixa.esperar(cliente["email"], "e-mail da sua conta na CLOUUD foi alterado")
    assert "tr***@exemplo.com" in aviso["texto"] and novo not in aviso["texto"]


def test_pagina_de_erro_nao_tem_texto_tecnico(paginas):
    pagina = paginas.nova()
    pagina.goto("/Home/Error")
    texto = pagina.inner_text("main")
    assert "Algo deu errado" in texto and "Development" not in texto


@pytest.mark.lento
def test_sem_pagamento_simulado_ninguem_aprova_o_proprio_pedido(app, fabrica, banco):
    """Em produção sem operadora de pagamento, os botões de simulação não valem."""
    url = "http://127.0.0.1:5106"
    loja = Aplicacao(url, ambiente_da_aplicacao(url, Pagamento__Simulado="false"), SAIDA / "app-sem-simulacao.log")
    try:
        assert loja.esperar()
        jogo = fabrica.jogo(chaves=1)
        cliente = fabrica.cliente()
        carrinho_com(banco, cliente, jogo["produto_id"])
        s = requests.Session()
        s.trust_env = False
        tok = lambda h: re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', h).group(1)
        r = s.get(url + "/conta/login")
        s.post(url + "/conta/login", data={"Email": cliente["email"], "Senha": cliente["senha"], "__RequestVerificationToken": tok(r.text)})
        r = s.post(url + "/Cliente/Carrinho/Finalizar", data={"__RequestVerificationToken": tok(s.get(url + "/Cliente/Carrinho").text)})
        pedido = r.url.rsplit("/", 1)[1]
        assert "pagamentoIndisponivel" in r.text and "simular aprovação" not in r.text
        s.post(url + f"/Cliente/Pedidos/Pagar/{pedido}", data={"metodo": "Pix", "aprovar": "true", "__RequestVerificationToken": tok(r.text)})
        assert banco.valor("SELECT status FROM pedidos WHERE id = %s", int(pedido)) == "AguardandoPagamento"
        assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Vendida'", jogo["produto_id"]) == 0
    finally:
        loja.parar()
