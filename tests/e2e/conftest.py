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
import base64
import hashlib
import hmac
import re
import shutil
import subprocess
import time
import uuid
from urllib.parse import urlparse
from pathlib import Path

from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from email import policy
from email.parser import BytesParser

import psycopg
import pytest
import requests

RAIZ = Path(__file__).resolve().parents[2]
PROJETO = RAIZ / "src" / "Clouud.Web"
UPLOADS = PROJETO / "wwwroot" / "uploads"
ARQUIVOS = Path(__file__).resolve().parent / "arquivos"
SAIDA = Path(__file__).resolve().parent / ".saida"
PASTA_EMAILS = SAIDA / "emails"

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

# Chave mestra só desta execução dos testes (em produção vem de Seguranca__ChaveCriptografia)
CHAVE_CRIPTOGRAFIA = base64.b64encode(os.urandom(32)).decode()

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


def hash_senha(senha: str) -> str:
    """Hash no formato do PasswordHasher do ASP.NET (v3, PBKDF2). Parâmetros leves de propósito:
    o login aceita e grava um hash novo, mais forte (como acontece com contas de versões antigas)."""
    import struct
    salt = os.urandom(16)
    iteracoes = 10_000
    subchave = hashlib.pbkdf2_hmac("sha256", senha.encode(), salt, iteracoes, 32)
    return base64.b64encode(b"\x01" + struct.pack(">III", 1, iteracoes, len(salt)) + salt + subchave).decode()


class Cofre:
    """O mesmo esquema de Infraestrutura/CriptografiaChaves.cs, para os testes gravarem e lerem chaves no banco."""

    def __init__(self, chave_mestra_base64: str):
        mestra = base64.b64decode(chave_mestra_base64)
        derivar = lambda info: HKDF(algorithm=hashes.SHA256(), length=32, salt=None, info=info).derive(mestra)
        self.cifra = AESGCM(derivar(b"clouud/chaves/cifra/v1"))
        self.busca = derivar(b"clouud/chaves/busca/v1")

    def cifrar(self, codigo: str) -> str:
        nonce = os.urandom(12)
        return "v1:" + base64.b64encode(nonce + self.cifra.encrypt(nonce, codigo.encode(), b"v1:")).decode()

    def decifrar(self, valor: str) -> str:
        dados = base64.b64decode(valor.removeprefix("v1:"))
        return self.cifra.decrypt(dados[:12], dados[12:], b"v1:").decode()

    def hash(self, codigo: str) -> str:
        return hmac.new(self.busca, codigo.encode(), hashlib.sha256).hexdigest()


COFRE = Cofre(CHAVE_CRIPTOGRAFIA)


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

    def codigos(self, where: str, *parametros) -> list[str]:
        """Códigos das chaves (decifrados), em ordem de id. Ex.: codigos("produto_id = %s", 10)."""
        return [COFRE.decifrar(c) for (c,) in self.linhas(
            f"SELECT codigo_cifrado FROM chaves c WHERE {where} ORDER BY c.id", *parametros)]


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


def ambiente_da_aplicacao(url: str = URL, banco: str = BANCO, **extras: str) -> dict:
    """Variáveis de ambiente da aplicação nos testes (os segredos vêm daqui, como em produção)."""
    ambiente = os.environ.copy()
    ambiente.update({
        "ASPNETCORE_ENVIRONMENT": "Development",
        "TZ": FUSO,
        "ConnectionStrings__LojaJogos": string_conexao(banco),
        "Banco__AplicarMigrations": "true",
        "AdminInicial__Email": ADMIN_EMAIL,
        "AdminInicial__Senha": ADMIN_SENHA,
        "Seguranca__ChaveCriptografia": CHAVE_CRIPTOGRAFIA,
        # e-mails viram arquivos .eml numa pasta que os testes leem; a fila é conferida a cada segundo
        "Email__Modo": "Pasta",
        "Email__Pasta": str(PASTA_EMAILS),
        "Email__IntervaloSegundos": "1",
        "Loja__AvisosIntervaloSegundos": "1",
        "Loja__UrlPublica": url,
        # os testes fazem centenas de logins do mesmo IP; os limites têm testes próprios, numa aplicação separada
        "Seguranca__Limites__LoginPorMinuto": "100000",
        "Seguranca__Limites__FormulariosDeContaPor15Minutos": "100000",
        "Seguranca__Limites__ComprasPorMinuto": "100000",
    })
    ambiente.update(extras)
    return ambiente


class Aplicacao:
    """Um processo da aplicação. Os testes de configuração sobem outras, em outras portas."""

    def __init__(self, url: str, ambiente: dict, log: Path):
        self.url = url
        self.log = log
        self.arquivo_log = open(log, "w")
        self.processo = subprocess.Popen(["dotnet", str(encontrar_dll()), "--urls", url],
                                         cwd=PROJETO, env=ambiente, stdout=self.arquivo_log, stderr=subprocess.STDOUT)

    def esperar(self, segundos: float = 120) -> bool:
        """Espera responder. False se o processo parou (ex.: configuração inválida)."""
        limite = time.time() + segundos
        while True:
            if self.processo.poll() is not None:
                return False
            try:
                requests.get(self.url, timeout=2, allow_redirects=False, verify=False)
                return True
            except requests.RequestException:
                pass
            if time.time() > limite:
                raise TimeoutError(f"A aplicação não respondeu. Veja {self.log}")
            time.sleep(0.3)

    def texto_do_log(self) -> str:
        self.arquivo_log.flush()
        return self.log.read_text()

    def parar(self):
        if self.processo.poll() is None:
            self.processo.terminate()
            try:
                self.processo.wait(timeout=20)
            except subprocess.TimeoutExpired:
                self.processo.kill()
        self.arquivo_log.close()


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
    shutil.rmtree(PASTA_EMAILS, ignore_errors=True)
    PASTA_EMAILS.mkdir(parents=True)
    aplicacao = Aplicacao(URL, ambiente_da_aplicacao(), SAIDA / "app.log")
    try:
        try:
            if not aplicacao.esperar() or requests.get(URL, timeout=10).status_code != 200:
                pytest.exit(f"A aplicação parou ao iniciar. Veja {SAIDA / 'app.log'}", returncode=2)
        except TimeoutError:
            pytest.exit(f"A aplicação não respondeu em 2 minutos. Veja {SAIDA / 'app.log'}", returncode=2)
        yield URL
    finally:
        aplicacao.parar()
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

    def cliente(self, nome: str = "Cliente", senha: str = "senha123", confirmado: bool = True) -> dict:
        email = f"{nome.lower().replace(' ', '')}.{self.sufixo()}@teste.com"
        usuario_id = self.banco.valor(
            """INSERT INTO usuarios (name, email, senha, perfil, email_confirmado_em)
               VALUES (%s, %s, %s, 0, CASE WHEN %s THEN now() END) RETURNING id""", nome, email, hash_senha(senha), confirmado)
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
                """INSERT INTO chaves (produto_id, codigo_cifrado, codigo_hash, status, adicionada_em)
                   VALUES (%s, %s, %s, 'Disponivel', now())""",
                produto_id, COFRE.cifrar(codigo), COFRE.hash(codigo))
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


# ---------------------------------------------------------------- e-mails

class Caixa:
    """Lê os e-mails que a loja gravou como .eml (modo "Pasta")."""

    @staticmethod
    def todos() -> list[dict]:
        emails = []
        for arquivo in sorted(PASTA_EMAILS.glob("*.eml"), key=lambda a: a.stat().st_mtime):
            mensagem = BytesParser(policy=policy.default).parsebytes(arquivo.read_bytes())
            html = mensagem.get_body(("html",))
            texto = mensagem.get_body(("plain",))
            emails.append({
                "para": str(mensagem["To"]),
                "de": str(mensagem["From"]),
                "assunto": str(mensagem["Subject"]),
                "html": html.get_content() if html else "",
                "texto": texto.get_content() if texto else "",
            })
        return emails

    def de(self, para: str) -> list[dict]:
        return [e for e in self.todos() if para.lower() in e["para"].lower()]

    def esperar(self, para: str, assunto: str = "", quantidade: int = 1, segundos: float = 15) -> list[dict]:
        """Espera chegarem `quantidade` e-mails para `para` com `assunto` no título (a fila roda a cada segundo)."""
        limite = time.time() + segundos
        while True:
            achados = [e for e in self.de(para) if assunto.lower() in e["assunto"].lower()]
            if len(achados) >= quantidade or time.time() > limite:
                assert len(achados) >= quantidade, f"Esperava {quantidade} e-mail(s) '{assunto}' para {para}; chegaram: " + \
                    str([e["assunto"] for e in self.de(para)])
                return achados
            time.sleep(0.3)

    @staticmethod
    def link(email: dict, contendo: str) -> str:
        """Primeiro link do e-mail cujo endereço contém `contendo`."""
        for url in re.findall(r'href="([^"]+)"', email["html"]):
            url = url.replace("&amp;", "&")
            if contendo in url:
                return url
        raise AssertionError(f"Link com '{contendo}' não encontrado no e-mail '{email['assunto']}'")


@pytest.fixture(scope="session")
def caixa(app) -> Caixa:
    return Caixa()


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
        # a Content-Security-Policy bloqueou algo da própria loja (script inline, handler onclick...)
        pagina.on("console", lambda m: "Content Security Policy" in m.text and self.erros.append(f"CSP: {m.text}"))
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
