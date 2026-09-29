# Testes e integração contínua

## Testes

Os testes em `tests/e2e` usam a aplicação de verdade: sobem o CLOUUD num banco só para testes (`clouud_testes`, apagado e recriado a cada execução; o `lojajogos` não é tocado) e usam um navegador Chromium (Playwright) para fazer o que um cliente e um admin fazem, conferindo o resultado nas telas, no banco e nos e-mails. Cobrem catálogo, vitrine, estoque, compra, cupons, avaliações, lista de desejos, conta e foto de perfil, galeria e capa, painel, e-mails, segurança (criptografia, login, sessão, HTTPS e cabeçalhos), as regras do banco e compras simultâneas (a mesma chave nunca é vendida duas vezes).

### Preparar (uma vez)

Precisa de Python 3.10 ou mais novo:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r tests/e2e/requirements.txt
python -m playwright install chromium
```

No Ubuntu/Pop!_OS, se o `venv` reclamar, instale antes com `sudo apt install python3-venv`; se o Chromium não abrir por falta de bibliotecas do sistema, rode `python -m playwright install --with-deps chromium`.

### Rodar

Com o PostgreSQL ligado em `localhost:5432`, usuário e senha `postgres`:

```bash
source .venv/bin/activate
dotnet build
python -m pytest tests/e2e                # todos (uns 5 minutos)
python -m pytest tests/e2e -m "not lento" # sem os demorados (compras simultâneas e instâncias extras da aplicação)
python -m pytest tests/e2e/test_cupons.py # um arquivo só
```

Outro servidor ou senha do banco: variáveis `CLOUUD_PG_HOST`, `CLOUUD_PG_PORTA`, `CLOUUD_PG_USUARIO` e `CLOUUD_PG_SENHA`. Quando um teste falha, o log da aplicação e capturas de tela das páginas ficam em `tests/e2e/.saida/`.

## Integração contínua

A cada `git push` (e em pull requests) o GitHub Actions (`.github/workflows/ci.yml`) compila o projeto, confere se algum pacote NuGet tem vulnerabilidade conhecida, sobe um PostgreSQL 17 e roda todos os testes. O Dependabot (`.github/dependabot.yml`) abre pull requests com as atualizações das dependências. O resultado aparece na aba **Actions** do repositório e no selo no topo do README; se algo falhar, o log e as capturas de tela ficam disponíveis para baixar na própria execução.
