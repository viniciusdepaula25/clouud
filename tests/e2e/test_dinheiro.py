"""Dinheiro no padrão brasileiro: exibição "R$ 1.234,50" e campos que aceitam vírgula ou ponto."""
import pytest

from conftest import LOJA, brl, enviar


@pytest.mark.parametrize("digitado, esperado", [
    ("59,90", "59.90"),
    ("59.90", "59.90"),
    ("1.234,50", "1234.50"),
    ("1234,5", "1234.50"),
    ("R$ 1.234,50", "1234.50"),
    ("1,234.50", "1234.50"),
    ("1.500", "1500.00"),     # ponto com 3 dígitos depois é milhar
    ("0.500", "0.50"),
    ("12.345.678,90", "12345678.90"),
    ("100", "100.00"),
])
def test_preco_aceita_virgula_ou_ponto(paginas, fabrica, banco, digitado, esperado):
    jogo = fabrica.jogo()
    admin = paginas.admin()
    admin.goto(f"/Admin/Produto/Inclui?jogoId={jogo['jogo_id']}")
    admin.select_option("#PlataformaId", label="GOG")
    admin.fill("#Preco", digitado)
    enviar(admin, "input[value=Cadastrar]")  # com a validação do navegador ligada
    assert "Produto cadastrado." in admin.inner_text("main"), admin.inner_text("main")
    assert banco.valor("""SELECT preco::text FROM produtos p JOIN plataformas pl ON pl.id = p.plataforma_id
                          WHERE p.jogo_id = %s AND pl.slug = 'gog'""", jogo["jogo_id"]) == esperado


@pytest.mark.parametrize("digitado", ["abc", "12,3,4", "1.23.4", "5,"])
def test_valor_invalido_e_recusado(paginas, fabrica, banco, digitado):
    jogo = fabrica.jogo()
    admin = paginas.admin()
    admin.goto(f"/Admin/Produto/Inclui?jogoId={jogo['jogo_id']}")
    admin.select_option("#PlataformaId", label="GOG")
    admin.fill("#Preco", digitado)
    admin.locator("#Preco").blur()
    assert admin.locator("span[data-valmsg-for=Preco]").inner_text() != ""  # o navegador avisa
    admin.evaluate("const f = document.querySelector('main form'); f.noValidate = true; f.submit()")
    admin.wait_for_load_state()
    assert "valor inválido" in admin.inner_text("main")  # e o servidor também recusa
    assert admin.input_value("#Preco") == digitado  # o que foi digitado volta para corrigir
    assert banco.valor("SELECT count(*) FROM produtos WHERE jogo_id = %s", jogo["jogo_id"]) == 1


def test_exibicao_e_formulario_de_alteracao(paginas, fabrica):
    jogo = fabrica.jogo(preco=1234.5, promocional=999.9, chaves=1)
    assert brl(1234.5) == "R$ 1.234,50"
    vitrine = paginas.nova()
    vitrine.goto(f"{LOJA}?busca={jogo['titulo']}")
    card = vitrine.locator(".card", has_text=jogo["titulo"]).first.inner_text()
    assert "R$ 1.234,50" in card and "R$ 999,90" in card

    admin = paginas.admin()
    admin.goto(f"/Admin/Produto/Altera/{jogo['produto_id']}")
    assert admin.input_value("#Preco") == "1234,50" and admin.input_value("#PrecoPromocional") == "999,90"
    assert admin.get_attribute("#Preco", "type") == "text" and admin.get_attribute("#Preco", "inputmode") == "decimal"
    enviar(admin, "input[value=Salvar]")  # salvar sem mexer mantém os valores
    admin.goto(f"/Admin/Produto/Altera/{jogo['produto_id']}")
    assert admin.input_value("#Preco") == "1234,50"


def test_cupom_com_virgula(paginas, fabrica, banco):
    codigo = f"MEIO{fabrica.sufixo().upper()}"
    admin = paginas.admin()
    admin.goto("/Admin/Cupons/Inclui")
    admin.fill("#Codigo", codigo)
    admin.select_option("#Tipo", "Percentual")
    admin.fill("#Valor", "12,5")
    admin.fill("#PedidoMinimo", "1.000,00")
    enviar(admin, "input[value=Cadastrar]")
    assert banco.linhas("SELECT valor::text, pedido_minimo::text FROM cupons WHERE codigo = %s", codigo) == [("12.50", "1000.00")]
    linha = admin.locator("tbody tr", has_text=codigo).inner_text()
    assert "12,5%" in linha and "R$ 1.000,00" in linha
