"""Cupons de desconto: cadastro no admin, validações, aplicação no carrinho, limites e pedido."""
import datetime

import pytest

from conftest import brl, enviar

HOJE = datetime.date.today()
ONTEM = HOJE - datetime.timedelta(days=1)
AMANHA = HOJE + datetime.timedelta(days=1)


@pytest.fixture
def admin(paginas):
    return paginas.admin()


def criar_cupom(admin, codigo, tipo="Percentual", valor="10", minimo="", de="", ate="", limite="", por_cliente="1",
                ativo=True) -> str:
    """Preenche o formulário (sem a validação do navegador, para testar a do servidor). Devolve o texto da página."""
    admin.goto("/Admin/Cupons/Inclui")
    admin.fill("#Codigo", codigo)
    admin.select_option("#Tipo", tipo)
    admin.fill("#Valor", valor)
    admin.fill("#PedidoMinimo", minimo)
    admin.fill("#ValidoDe", de)
    admin.fill("#ValidoAte", ate)
    admin.fill("#LimiteUsos", limite)
    admin.fill("#LimitePorCliente", por_cliente)
    if not ativo:
        admin.uncheck("#Ativo")
    with admin.expect_navigation():
        admin.evaluate("const f = document.querySelector('main form'); f.noValidate = true; f.submit()")
    return admin.inner_text("main")


def novo_codigo(fabrica, prefixo):
    return f"{prefixo}{fabrica.sufixo().upper()}"


def test_cadastro_e_validacoes(admin, fabrica, banco):
    assert admin.locator("nav a", has_text="Cupons").count() == 1
    codigo = novo_codigo(fabrica, "BEMVINDO")
    assert "cadastrado" in criar_cupom(admin, codigo.lower())
    assert banco.linhas("SELECT tipo, valor FROM cupons WHERE codigo = %s", codigo) == [("Percentual", 10)]  # em maiúsculo
    assert "Já existe um cupom" in criar_cupom(admin, codigo)
    assert "vai até 100" in criar_cupom(admin, novo_codigo(fabrica, "DEMAIS"), valor="150")
    assert "depois da inicial" in criar_cupom(admin, novo_codigo(fabrica, "DATAS"), de=HOJE.isoformat(), ate=ONTEM.isoformat())
    assert "letras, números e hífen" in criar_cupom(admin, "com espaço")

    cupom_id = banco.valor("SELECT id FROM cupons WHERE codigo = %s", codigo)
    admin.goto(f"/Admin/Cupons/Altera/{cupom_id}")
    admin.fill("#Valor", "15")
    enviar(admin, "input[value=Salvar]")
    assert banco.valor("SELECT valor FROM cupons WHERE id = %s", cupom_id) == 15


class Carrinho:
    def __init__(self, pagina, banco, cliente, produto_id):
        self.pagina, self.banco, self.cliente, self.produto_id = pagina, banco, cliente, produto_id

    def encher(self, quantidade=1):
        self.banco.executar("DELETE FROM carrinho_itens WHERE usuario_id = %s", self.cliente["id"])
        self.banco.executar("""INSERT INTO carrinho_itens (usuario_id, produto_id, quantidade, adicionado_em)
                               VALUES (%s, %s, %s, now())""", self.cliente["id"], self.produto_id, quantidade)
        self.pagina.goto("/Cliente/Carrinho")

    def aplicar(self, codigo):
        self.pagina.fill("input[name=codigo]", codigo)
        enviar(self.pagina, "input[value=Aplicar]")

    def remover_cupom(self):
        if self.pagina.locator("form[action*=RemoverCupom]").count():
            enviar(self.pagina, "form[action*=RemoverCupom] input[type=submit]")

    def quantidade(self, n):
        with self.pagina.expect_navigation():
            self.pagina.select_option("select[name=quantidade]", str(n))

    def mensagens(self):
        p = self.pagina
        return " ".join(p.locator(s).inner_text() for s in (".alert-danger", "#avisoCupom") if p.locator(s).count())

    def total(self):
        return self.pagina.inner_text("#totalCarrinho")

    def finalizar(self):
        enviar(self.pagina, "input[value='Finalizar compra']")


@pytest.fixture
def carrinho(paginas, fabrica, banco):
    jogo = fabrica.jogo(preco=249.90, chaves=5)
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    c = Carrinho(pagina, banco, cliente, jogo["produto_id"])
    c.encher()
    return c


def test_cupons_que_nao_valem(admin, fabrica, carrinho):
    carrinho.aplicar("NAOEXISTE")
    assert "não existe" in carrinho.mensagens() and carrinho.pagina.locator("#cupomAplicado").count() == 0
    casos = [
        (dict(ate=ONTEM.isoformat()), "venceu"),
        (dict(de=AMANHA.isoformat()), "só vale a partir"),
        (dict(ativo=False), "não está mais ativo"),
    ]
    for opcoes, mensagem in casos:
        codigo = novo_codigo(fabrica, "X")
        criar_cupom(admin, codigo, **opcoes)
        carrinho.pagina.goto("/Cliente/Carrinho")
        carrinho.aplicar(codigo)
        assert mensagem in carrinho.mensagens() and carrinho.total() == brl(249.90), codigo
        carrinho.remover_cupom()


def test_percentual_no_carrinho_no_pedido_e_limite_por_cliente(admin, fabrica, banco, carrinho):
    codigo = novo_codigo(fabrica, "DEZ")
    criar_cupom(admin, codigo)
    pagina = carrinho.pagina
    pagina.goto("/Cliente/Carrinho")
    carrinho.aplicar(f" {codigo.lower()} ")
    assert "aplicado" in pagina.inner_text(".alert-success")
    assert pagina.inner_text("#descontoCarrinho") == "- " + brl(24.99) and carrinho.total() == brl(224.91)
    carrinho.quantidade(2)
    assert pagina.inner_text("#descontoCarrinho") == "- " + brl(49.98) and carrinho.total() == brl(449.82)
    carrinho.quantidade(1)

    carrinho.finalizar()
    pedido = int(pagina.url.rsplit("/", 1)[1])
    assert banco.linhas("""SELECT p.subtotal::text, p.desconto::text, p.total::text, c.codigo
                           FROM pedidos p JOIN cupons c ON c.id = p.cupom_id WHERE p.id = %s""", pedido) == [
        ("249.90", "24.99", "224.91", codigo)]
    assert f"Pagar {brl(224.91)}" in pagina.inner_text("main") and f"cupom {codigo}" in pagina.inner_text("main")
    pagina.goto("/Cliente/Carrinho")
    assert pagina.locator("#cupomAplicado").count() == 0  # o cupom sai do carrinho depois de finalizar

    # limite de 1 por cliente: pedido aguardando pagamento já conta como uso
    carrinho.encher()
    carrinho.aplicar(codigo)
    assert f"Você já usou o cupom {codigo}" in carrinho.mensagens()
    carrinho.finalizar()
    assert "Você já usou" in pagina.inner_text(".alert-danger") and "/Carrinho" in pagina.url

    # cancelar o pedido devolve o uso
    pagina.goto(f"/Cliente/Pedidos/Pagar/{pedido}")
    enviar(pagina, "input[value='Cancelar pedido']")
    pagina.goto("/Cliente/Carrinho")
    assert pagina.locator("#avisoCupom").count() == 0 and carrinho.total() == brl(224.91)


def test_pedido_minimo_e_cupom_desativado_antes_de_finalizar(admin, fabrica, banco, carrinho):
    codigo = novo_codigo(fabrica, "FIXO")
    criar_cupom(admin, codigo, tipo="ValorFixo", valor="50", minimo="300")
    carrinho.pagina.goto("/Cliente/Carrinho")
    carrinho.aplicar(codigo)
    assert f"a partir de {brl(300)}" in carrinho.mensagens() and carrinho.total() == brl(249.90)
    carrinho.quantidade(2)
    assert carrinho.pagina.locator("#avisoCupom").count() == 0
    assert carrinho.pagina.inner_text("#descontoCarrinho") == "- " + brl(50) and carrinho.total() == brl(449.80)

    banco.executar("UPDATE cupons SET ativo = false WHERE codigo = %s", codigo)
    carrinho.finalizar()
    assert "não está mais ativo" in carrinho.pagina.inner_text(".alert-danger")
    assert banco.valor("SELECT count(*) FROM pedidos WHERE usuario_id = %s", carrinho.cliente["id"]) == 0
    assert banco.valor("SELECT count(*) FROM chaves WHERE produto_id = %s AND status = 'Reservada'", carrinho.produto_id) == 0


def test_cupom_de_100_por_cento_relatorio_e_exclusao(admin, fabrica, banco, carrinho):
    gratis, sem_uso = novo_codigo(fabrica, "GRATIS"), novo_codigo(fabrica, "SEMUSO")
    criar_cupom(admin, gratis, valor="100", por_cliente="")
    criar_cupom(admin, sem_uso, ativo=False)
    criar_cupom(admin, vencido := novo_codigo(fabrica, "VENCIDO"), ate=ONTEM.isoformat())
    carrinho.pagina.goto("/Cliente/Carrinho")
    carrinho.aplicar(gratis)
    assert carrinho.total() == brl(0)
    carrinho.finalizar()
    enviar(carrinho.pagina, "button[value=true]")
    assert "Pagamento aprovado" in carrinho.pagina.inner_text("main")
    assert carrinho.pagina.locator("code[id^=chave-]").count() == 1
    pedido = carrinho.pagina.url.rsplit("/", 1)[1]

    admin.goto(f"/Admin/Pedidos/Detalhes/{pedido}")
    assert f"cupom {gratis}" in admin.inner_text("main") and brl(249.90) in admin.inner_text("main")
    admin.goto("/Admin/Cupons")
    linha = admin.locator("tbody tr", has_text=gratis)
    assert "1" in linha.locator("td").nth(4).inner_text() and brl(249.90) in linha.inner_text()
    assert "Vencido" in admin.locator("tbody tr", has_text=vencido).inner_text()
    assert "Inativo" in admin.locator("tbody tr", has_text=sem_uso).inner_text()

    admin.goto(f"/Admin/Cupons/Exclui/{banco.valor('SELECT id FROM cupons WHERE codigo = %s', gratis)}")
    enviar(admin, "input[value=Excluir]")
    assert "já foi usado" in admin.inner_text("main")
    admin.goto(f"/Admin/Cupons/Exclui/{banco.valor('SELECT id FROM cupons WHERE codigo = %s', sem_uso)}")
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main")
    assert banco.valor("SELECT count(*) FROM cupons WHERE codigo = %s", sem_uso) == 0
