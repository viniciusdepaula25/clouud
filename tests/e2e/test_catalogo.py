"""Admin: categorias, plataformas, jogos (com capa) e produtos (com promoção)."""
from conftest import ARQUIVOS, brl, enviar


def test_categorias_incluir_renomear_duplicada_e_excluir(paginas, fabrica):
    admin = paginas.admin()
    nome = f"Categoria {fabrica.sufixo()}"
    admin.goto("/Admin/Categoria")
    admin.fill("input[placeholder='Nova categoria']", nome)
    enviar(admin, "input[value=Adicionar]")
    assert f'Categoria "{nome}" cadastrada.' in admin.inner_text("main")

    admin.fill("input[placeholder='Nova categoria']", nome.upper())
    enviar(admin, "input[value=Adicionar]")
    assert "já existe" in admin.inner_text(".alert-danger")

    linha = admin.locator("tr", has=admin.locator(f"input[value='{nome}']"))
    linha.locator("input[name=nome]").fill(nome + " nova")
    with admin.expect_navigation():
        linha.locator("input[value=Renomear]").click()
    assert f'renomeada para "{nome} nova"' in admin.inner_text("main")

    linha = admin.locator("tr", has=admin.locator(f"input[value='{nome} nova']"))
    with admin.expect_navigation():
        linha.locator("input[value=Excluir]").click()
    assert "excluída" in admin.inner_text("main")


def test_plataformas_incluir_duplicada_e_exclusao_bloqueada(paginas, fabrica, banco):
    admin = paginas.admin()
    nome = f"Launcher {fabrica.sufixo()}"
    admin.goto("/Admin/Plataforma/Inclui")
    admin.fill("#Nome", nome)
    admin.fill("#InstrucoesAtivacao", "Abra o launcher e ative.")
    enviar(admin, "input[value=Cadastrar]")
    assert f'"{nome}" cadastrada' in admin.inner_text("main")

    admin.goto("/Admin/Plataforma/Inclui")
    admin.fill("#Nome", "STEAM")
    enviar(admin, "input[value=Cadastrar]")
    assert "Já existe uma plataforma" in admin.inner_text("main")

    fabrica.jogo()  # garante que a Steam tem produto
    steam = banco.valor("SELECT id FROM plataformas WHERE slug = 'steam'")
    admin.goto(f"/Admin/Plataforma/Exclui/{steam}")
    enviar(admin, "input[value=Excluir]")
    assert "não pode ser excluída" in admin.inner_text("main")


def test_jogo_com_capa_categorias_e_produto_com_promocao(paginas, fabrica, banco):
    admin = paginas.admin()
    titulo = f"Hollow {fabrica.sufixo()}"
    admin.goto("/Admin/Jogo/Inclui")
    admin.fill("#Titulo", titulo)
    admin.fill("#Descricao", "Metroidvania.\nSegunda linha.")
    admin.fill("#DataLancamento", "2017-02-24")
    admin.select_option("#ClassificacaoIndicativa", "10")
    admin.fill("#Desenvolvedora", f"Estúdio {fabrica.sufixo()}")
    admin.check("#categoria-" + str(banco.valor("SELECT id FROM categorias WHERE slug = 'indie'")))
    admin.check("#categoria-" + str(banco.valor("SELECT id FROM categorias WHERE slug = 'aventura'")))
    admin.check("#Destaque")
    admin.set_input_files("#txtArquivo", str(ARQUIVOS / "imagem.png"))
    enviar(admin, "input[value=Cadastrar]")

    # depois de cadastrar, vai direto para o cadastro do produto, com o jogo escolhido
    assert "/Admin/Produto/Inclui" in admin.url
    assert admin.locator("#JogoId option:checked").inner_text() == titulo
    jogo_id = banco.valor("SELECT id FROM jogos WHERE titulo = %s", titulo)
    capa = banco.valor("SELECT capa FROM jogos WHERE id = %s", jogo_id)
    assert capa.startswith("capas/") and capa.endswith(".png")

    # promoção maior que o preço é recusada pelo servidor
    admin.select_option("#PlataformaId", label="Steam")
    admin.fill("#Preco", "60.00")
    admin.fill("#PrecoPromocional", "70.00")
    admin.evaluate("document.querySelector('main form').noValidate = true")
    with admin.expect_navigation():
        admin.evaluate("document.querySelector('main form').submit()")
    assert "menor que o preço normal" in admin.inner_text("main")

    admin.fill("#Preco", "60.00")
    admin.fill("#PrecoPromocional", "30.00")
    admin.fill("#PromocaoAte", "2099-12-31")
    enviar(admin, "input[value=Cadastrar]")
    texto = admin.inner_text("main")
    assert "Produto cadastrado." in texto and "R$ 60.00" in texto and "R$ 30.00" in texto and "31/12/2099" in texto

    # mesmo jogo + plataforma + edição não repete
    admin.goto(f"/Admin/Produto/Inclui?jogoId={jogo_id}")
    admin.select_option("#PlataformaId", label="Steam")
    admin.fill("#Preco", "10")
    enviar(admin, "input[value=Cadastrar]")
    assert "Já existe um produto" in admin.inner_text("main")

    # a alteração vem com os campos preenchidos
    admin.goto(f"/Admin/Jogo/Altera/{jogo_id}")
    assert admin.input_value("#DataLancamento") == "2017-02-24"
    assert admin.input_value("#ClassificacaoIndicativa") == "10"
    assert admin.locator("input[name=CategoriaIds]:checked").count() == 2
    assert admin.locator("#Destaque").is_checked()

    # exibe e capa servida
    admin.goto(f"/Admin/Jogo/Exibe/{jogo_id}")
    texto = admin.inner_text("main")
    assert "24/02/2017" in texto and "Aventura, Indie" in texto
    assert admin.request.get(admin.locator("img[alt=Capa]").get_attribute("src")).status == 200

    # vitrine: selo de desconto e preço promocional
    vitrine = paginas.nova()
    vitrine.goto(f"/?busca={titulo}")
    card = vitrine.locator(".card").first
    assert "-50%" in card.inner_text() and brl(30) in card.inner_text()


def test_busca_no_admin(paginas, fabrica):
    jogo = fabrica.jogo()
    admin = paginas.admin()
    admin.goto("/Admin/Jogo")
    admin.fill("#txtBusca", jogo["titulo"])
    enviar(admin, "input[value=Buscar]")
    assert admin.locator("tbody tr").count() == 1


def test_jogo_com_produto_nao_e_excluido_mas_sem_produto_e(paginas, fabrica, banco):
    com_produto = fabrica.jogo()
    admin = paginas.admin()
    admin.goto(f"/Admin/Jogo/Exclui/{com_produto['jogo_id']}")
    enviar(admin, "input[value=Excluir]")
    assert "não pode ser excluído" in admin.inner_text("main")

    banco.executar("DELETE FROM produtos WHERE id = %s", com_produto["produto_id"])
    admin.goto(f"/Admin/Jogo/Exclui/{com_produto['jogo_id']}")
    enviar(admin, "input[value=Excluir]")
    assert "excluído" in admin.inner_text("main")
    assert banco.valor("SELECT count(*) FROM jogos WHERE id = %s", com_produto["jogo_id"]) == 0


def test_cliente_nao_acessa_o_admin(paginas, fabrica):
    cliente = fabrica.cliente()
    pagina = paginas.logada(cliente["email"], cliente["senha"])
    resposta = pagina.request.get("/Admin/Produto", max_redirects=0)
    assert resposta.status == 302
