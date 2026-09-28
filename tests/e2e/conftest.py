"""
Base dos testes de ponta a ponta do CLOUUD.

Cada execução:
  1. recria o banco de testes (padrão: clouud_testes; o banco do dia a dia não é tocado);
  2. sobe a aplicação já compilada com Banco:AplicarMigrations=true (as migrations criam as tabelas);
  3. abre um Chromium (Playwright) e roda os testes contra http://127.0.0.1:5095;
  4. no fim, para a aplicação e apaga os arquivos que os testes enviaram para wwwroot/uploads.

Antes de rodar: dotnet build (ver o README, seção "Testes").
"""
import os
import re
import subprocess
import time
import uuid
from urllib.parse import urlparse
from pathlib import Path

import psycopg
import pytest
import requests

RAIZ = Path(__file__).resolve().parents[2]
PROJETO = RAIZ / "src" / "Clouud.Web"
UPLOADS = PROJETO / "wwwroot" / "uploads"
ARQUIVOS = Path(__file__).resolve().parent / "arquivos"
SAIDA = Path(__file__).resolve().parent / ".saida"

URL = os.environ.get("CLOUUD_URL", "http://127.0.0.1:5095")
PG_HOST = os.environ.get("CLOUUD_PG_HOST", "localhost")
PG_PORTA = os.environ.get("CLOUUD_PG_PORTA", "5432")
PG_USUARIO = os.environ.get("CLOUUD_PG_USUARIO", "postgres")
PG_SENHA = os.environ.get("CLOUUD_PG_SENHA", "postgres")
BANCO = os.environ.get("CLOUUD_BANCO_TESTE", "clouud_testes")

# Endereço da loja (vitrine com busca e filtros), a mesma para visitante e cliente
LOJA = "/loja"

# Fuso fixo para a aplicação: o painel conta "dias" no horário local, e os testes calculam do mesmo jeito no SQL
FUSO = "America/Sao_Paulo"

ADMIN_EMAIL = "admin@clouud.com"
ADMIN_SENHA = "Admin@123"

if BANCO in ("lojajogos", "postgres"):
    raise SystemExit(f"CLOUUD_BANCO_TESTE={BANCO}: use um banco só para testes (ele é apagado a cada execução).")


# ---------------------------------------------------------------- banco

def conectar(banco: str = BANCO) -> psycopg.Connection:
    return psycopg.connect(host=PG_HOST, port=PG_PORTA, user=PG_USUARIO, password=PG_SENHA, dbname=banco, autocommit=True)


def recriar_banco(nome: str) -> None:
    with conectar("postgres") as c:
        c.execute(f'DROP DATABASE IF EXISTS "{nome}" WITH (FORCE)')
        c.execute(f'CREATE DATABASE "{nome}"')


def string_conexao(banco: str = BANCO) -> str:
    return f"Host={PG_HOST};Port={PG_PORTA};Database={banco};Username={PG_USUARIO};Password={PG_SENHA}"


class Banco:
    """Acesso direto ao banco de testes, para preparar dados e conferir o resultado."""

    def __init__(self, conexao: psycopg.Connection):
        self.conexao = conexao

    def executar(self, sql: str, *parametros) -> None:
        self.conexao.execute(sql, parametros or None)

    def linhas(self, sql: str, *parametros) -> list[tuple]:
        return self.conexao.execute(sql, parametros or None).fetchall()

    def valor(self, sql: str, *parametros):
        linha = self.conexao.execute(sql, parametros or None).fetchone()
        return None if linha is None else linha[0]


# ---------------------------------------------------------------- aplicação

def encontrar_dll() -> Path:
    if os.environ.get("CLOUUD_DLL"):
        return Path(os.environ["CLOUUD_DLL"]).resolve()
    candidatas = [PROJETO / "bin" / c / "net10.0" / "Clouud.Web.dll" for c in ("Release", "Debug")]
    existentes = [c for c in candidatas if c.exists()]
    if not existentes:
        pytest.exit("Aplicação não compilada. Rode antes: dotnet build", returncode=2)
    return max(existentes, key=lambda c: c.stat().st_mtime)


def arquivos_em_uploads() -> set[Path]:
    return {p for p in UPLOADS.rglob("*") if p.is_file()} if UPLOADS.exists() else set()


@pytest.fixture(scope="session")
def app():
    """Sobe a aplicação num banco de testes novo. Devolve a URL."""
    try:
        recriar_banco(BANCO)
    except psycopg.OperationalError as erro:
        pytest.exit(f"Não consegui conectar ao PostgreSQL em {PG_HOST}:{PG_PORTA} (usuário {PG_USUARIO}). "
                    f"Ele está rodando? Detalhe: {erro}", returncode=2)
    antes = arquivos_em_uploads()
    SAIDA.mkdir(exist_ok=True)
    log = open(SAIDA / "app.log", "w")
    ambiente = os.environ.copy()
    ambiente.update({
        "ASPNETCORE_ENVIRONMENT": "Development",
        "TZ": FUSO,
        "ConnectionStrings__LojaJogos": string_conexao(),
        "Banco__AplicarMigrations": "true",
        "AdminInicial__Email": ADMIN_EMAIL,
        "AdminInicial__Senha": ADMIN_SENHA,
    })
    processo = subprocess.Popen(["dotnet", str(encontrar_dll()), "--urls", URL],
                                cwd=PROJETO, env=ambiente, stdout=log, stderr=subprocess.STDOUT)
    try:
        limite = time.time() + 120
        while True:
            if processo.poll() is not None:
                pytest.exit(f"A aplicação parou ao iniciar. Veja {SAIDA / 'app.log'}", returncode=2)
            try:
                if requests.get(URL, timeout=2).status_code == 200:
                    break
            except requests.RequestException:
                pass
            if time.time() > limite:
                pytest.exit(f"A aplicação não respondeu em 2 minutos. Veja {SAIDA / 'app.log'}", returncode=2)
            time.sleep(0.5)
        yield URL
    finally:
        processo.terminate()
        try:
            processo.wait(timeout=20)
        except subprocess.TimeoutExpired:
            processo.kill()
        log.close()
        for arquivo in arquivos_em_uploads() - antes:
            arquivo.unlink(missing_ok=True)


@pytest.fixture(scope="session")
def banco(app) -> Banco:
    with conectar() as conexao:
        yield Banco(conexao)


# ---------------------------------------------------------------- dados de teste

class Fabrica:
    """Cria dados direto no banco, sempre com nomes únicos, para cada teste não depender dos outros."""

    def __init__(self, banco: Banco):
        self.banco = banco

    @staticmethod
    def sufixo() -> str:
        return uuid.uuid4().hex[:8]

    def cliente(self, nome: str = "Cliente", senha: str = "senha123") -> dict:
        # Senha em texto puro de propósito: é o formato das contas antigas, e o login converte para hash
        email = f"{nome.lower().replace(' ', '')}.{self.sufixo()}@teste.com"
        usuario_id = self.banco.valor(
            "INSERT INTO usuarios (name, email, senha, perfil) VALUES (%s, %s, %s, 0) RETURNING id", nome, email, senha)
        return {"id": usuario_id, "nome": nome, "email": email, "senha": senha}

    def jogo(self, titulo: str | None = None, plataforma: str = "steam", preco: float = 100, chaves: int = 0,
             promocional: float | None = None, categorias: tuple[str, ...] = (), destaque: bool = False,
             edicao: str = "Standard") -> dict:
        titulo = titulo or f"Jogo {self.sufixo()}"
        slug = re.sub(r"[^a-z0-9]+", "-", titulo.lower()).strip("-") + "-" + self.sufixo()
        jogo_id = self.banco.valor(
            "INSERT INTO jogos (titulo, slug, ativo, destaque) VALUES (%s, %s, true, %s) RETURNING id", titulo, slug, destaque)
        for categoria in categorias:
            self.banco.executar(
                "INSERT INTO jogo_categorias (jogo_id, categoria_id) SELECT %s, id FROM categorias WHERE slug = %s",
                jogo_id, categoria)
        produto_id = self.produto(jogo_id, plataforma, preco, promocional, edicao)
        if chaves:
            self.chaves(produto_id, chaves)
        return {"jogo_id": jogo_id, "produto_id": produto_id, "titulo": titulo, "slug": slug}

    def produto(self, jogo_id: int, plataforma: str = "steam", preco: float = 100,
                promocional: float | None = None, edicao: str = "Standard") -> int:
        return self.banco.valor(
            """INSERT INTO produtos (jogo_id, plataforma_id, edicao, regiao, preco, preco_promocional, ativo)
               VALUES (%s, (SELECT id FROM plataformas WHERE slug = %s), %s, 'Global', %s, %s, true) RETURNING id""",
            jogo_id, plataforma, edicao, preco, promocional)

    def chaves(self, produto_id: int, quantidade: int) -> list[str]:
        codigos = [f"K{self.sufixo().upper()}-{i:03d}" for i in range(quantidade)]
        for codigo in codigos:
            self.banco.executar(
                "INSERT INTO chaves (produto_id, codigo, status, adicionada_em) VALUES (%s, %s, 'Disponivel', now())",
                produto_id, codigo)
        return codigos

    def pedido_pago(self, usuario_id: int, produto_id: int, quantidade: int = 1, preco: float = 100,
                    status: str = "Pago", pago_ha_dias: int = 0) -> int:
        total = preco * quantidade
        pedido_id = self.banco.valor(
            """INSERT INTO pedidos (usuario_id, status, subtotal, desconto, total, criado_em, pago_em)
               VALUES (%s, %s, %s, 0, %s, now() - make_interval(days => %s), now() - make_interval(days => %s))
               RETURNING id""", usuario_id, status, total, total, pago_ha_dias, pago_ha_dias)
        self.banco.executar(
            "INSERT INTO pedido_itens (pedido_id, produto_id, quantidade, preco_unitario, subtotal) VALUES (%s, %s, %s, %s, %s)",
            pedido_id, produto_id, quantidade, preco, total)
        return pedido_id


@pytest.fixture(scope="session")
def fabrica(banco) -> Fabrica:
    return Fabrica(banco)


@pytest.fixture(scope="session")
def arquivo_grande(tmp_path_factory) -> Path:
    """PNG de 6 MB (acima dos limites de 2 MB e 5 MB), gerado na hora para não ir para o Git."""
    caminho = tmp_path_factory.mktemp("arquivos") / "grande.png"
    caminho.write_bytes(b"\x89PNG\r\n\x1a\n" + bytes(6 * 1024 * 1024))
    return caminho


# ---------------------------------------------------------------- navegador

class Paginas:
    """Abre páginas, cada uma com sessão própria (cookies separados), e registra erros do servidor e de JavaScript."""

    def __init__(self, browser, url: str):
        self.browser = browser
        self.url = url
        self.contextos = []
        self.erros: list[str] = []

    def nova(self):
        contexto = self.browser.new_context(base_url=self.url, viewport={"width": 1280, "height": 900})
        self.contextos.append(contexto)
        pagina = contexto.new_page()
        pagina.on("response", lambda r: r.status >= 500 and self.erros.append(f"{r.status} {r.url}"))
        pagina.on("pageerror", lambda e: self.erros.append(f"JavaScript: {e}"))
        # confirm() das telas (ex.: "Excluir?") é aceito; um alert() inesperado é erro (ex.: script injetado)
        pagina.on("dialog", lambda d: d.accept() if d.type != "alert" else (self.erros.append(f"alert: {d.message}"), d.dismiss()))
        return pagina

    def logada(self, email: str, senha: str):
        pagina = self.nova()
        entrar(pagina, email, senha)
        return pagina

    def admin(self):
        return self.logada(ADMIN_EMAIL, ADMIN_SENHA)

    def fechar(self):
        for contexto in self.contextos:
            contexto.close()


@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_makereport(item, call):
    resultado = (yield).get_result()
    if resultado.when == "call":
        item.falhou = resultado.failed


@pytest.fixture
def paginas(browser, app, request):
    gerenciador = Paginas(browser, app)
    yield gerenciador
    if getattr(request.node, "falhou", False):
        # captura de tela de cada página aberta, para entender a falha (no CI vira artefato para baixar)
        pasta = SAIDA / "falhas" / re.sub(r"[^\w.-]+", "_", request.node.name)
        pasta.mkdir(parents=True, exist_ok=True)
        for i, contexto in enumerate(gerenciador.contextos):
            for j, pagina in enumerate(contexto.pages):
                try:
                    pagina.screenshot(path=pasta / f"pagina{i}-{j}.png", full_page=True)
                except Exception:
                    pass
    gerenciador.fechar()
    assert not gerenciador.erros, "Erros durante o teste: " + "; ".join(gerenciador.erros)


def entrar(pagina, email: str, senha: str, url: str = "/conta/login") -> None:
    pagina.goto(url)
    pagina.fill("input[name=Email]", email)
    pagina.fill("input[name=Senha]", senha)
    with pagina.expect_navigation():
        pagina.locator("form input[type=submit]").last.click()


def enviar(pagina, seletor: str) -> None:
    """Clica e espera a próxima página carregar."""
    with pagina.expect_navigation():
        pagina.click(seletor)


def token(pagina) -> str:
    """Token antiforgery da página atual (para mandar POSTs direto, sem o formulário)."""
    return pagina.locator("input[name=__RequestVerificationToken]").first.get_attribute("value")


def caminho(pagina) -> str:
    """Só o caminho da URL atual (ex.: "/" ou "/Cliente/Pedidos"), sem o endereço do servidor e sem a busca."""
    return urlparse(pagina.url).path


def brl(valor) -> str:
    """Formato de moeda da loja: R$ 1.234,50."""
    return "R$ " + f"{float(valor):,.2f}".replace(",", "_").replace(".", ",").replace("_", ".")
