"""
Concorrência: vários clientes finalizando a compra no mesmo instante.

Sem o navegador (usa requests + threads), para disparar os pedidos juntos de verdade.
Confere que nenhuma chave é vendida duas vezes e que um cupom de uso único é usado uma vez só.
"""
import re
import threading

import pytest
import requests

N_CLIENTES = 8
RODADAS = 3

pytestmark = pytest.mark.lento


def token_de(html: str) -> str:
    return re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html).group(1)


def sessao_logada(url, cliente) -> requests.Session:
    s = requests.Session()
    s.trust_env = False  # sem proxy: a aplicação é local
    r = s.get(url + "/conta/login")
    s.post(url + "/conta/login", data={"Email": cliente["email"], "Senha": cliente["senha"],
                                       "__RequestVerificationToken": token_de(r.text)})
    assert "/Cliente" in s.get(url + "/Cliente/Home").url
    return s


def adicionar_ao_carrinho(url, sessao, produto_id, cupom=None) -> str:
    """Põe 1 unidade no carrinho (e o cupom, se houver). Devolve o token para finalizar."""
    h = sessao.get(url + "/Cliente/Home").text
    r = sessao.post(url + "/Cliente/Carrinho/Adicionar", allow_redirects=False,
                    data={"produtoId": produto_id, "__RequestVerificationToken": token_de(h)})
    assert "/Cliente/Carrinho" in r.headers.get("Location", "")
    if cupom:
        h = sessao.get(url + "/Cliente/Carrinho").text
        sessao.post(url + "/Cliente/Carrinho/AplicarCupom", allow_redirects=False,
                    data={"codigo": cupom, "__RequestVerificationToken": token_de(h)})
    return token_de(sessao.get(url + "/Cliente/Carrinho").text)


def finalizar_todos_juntos(url, sessoes, tokens) -> list[str]:
    barreira = threading.Barrier(len(sessoes))
    resultados = [""] * len(sessoes)

    def finalizar(i):
        barreira.wait()
        r = sessoes[i].post(url + "/Cliente/Carrinho/Finalizar", allow_redirects=False,
                            data={"__RequestVerificationToken": tokens[i]})
        resultados[i] = r.headers.get("Location", str(r.status_code))

    threads = [threading.Thread(target=finalizar, args=(i,)) for i in range(len(sessoes))]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    return resultados


@pytest.fixture
def sessoes(app, fabrica):
    lista = [sessao_logada(app, fabrica.cliente(f"Concorrente{i}")) for i in range(N_CLIENTES)]
    yield lista
    for s in lista:
        s.close()


@pytest.mark.parametrize("rodada", range(RODADAS))
def test_chave_nunca_e_vendida_duas_vezes(app, fabrica, banco, sessoes, rodada):
    chaves = 3
    jogo = fabrica.jogo(chaves=chaves)
    tokens = [adicionar_ao_carrinho(app, s, jogo["produto_id"]) for s in sessoes]
    resultados = finalizar_todos_juntos(app, sessoes, tokens)

    assert sum("/Pedidos/Pagar/" in r for r in resultados) == chaves, resultados
    assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Reservada'", jogo["produto_id"]) == chaves
    # cada pedido criado tem exatamente as chaves que comprou, e nenhuma chave está em dois itens
    assert banco.valor("""SELECT count(*) FROM pedido_itens i
                          WHERE i.produto_id = %s
                            AND (SELECT count(*) FROM chaves c WHERE c.pedido_item_id = i.id) <> i.quantidade""",
                       jogo["produto_id"]) == 0
    assert banco.valor("SELECT count(*) FROM pedido_itens WHERE produto_id = %s", jogo["produto_id"]) == chaves


@pytest.mark.parametrize("rodada", range(RODADAS))
def test_cupom_de_uso_unico_e_usado_uma_vez(app, fabrica, banco, sessoes, rodada):
    jogo = fabrica.jogo(chaves=N_CLIENTES)
    codigo = f"UNICO{fabrica.sufixo().upper()}"
    banco.executar("""INSERT INTO cupons (codigo, tipo, valor, limite_usos, ativo, criado_em)
                      VALUES (%s, 'ValorFixo', 10, 1, true, now())""", codigo)
    tokens = [adicionar_ao_carrinho(app, s, jogo["produto_id"], codigo) for s in sessoes]
    resultados = finalizar_todos_juntos(app, sessoes, tokens)

    assert sum("/Pedidos/Pagar/" in r for r in resultados) == 1, resultados
    assert banco.valor("SELECT count(*) FROM pedidos p JOIN cupons c ON c.id = p.cupom_id WHERE c.codigo = %s", codigo) == 1
    # quem não conseguiu não deixou chave presa
    assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Reservada'", jogo["produto_id"]) == 1
