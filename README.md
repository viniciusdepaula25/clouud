# CLOUUD — Loja de chaves de jogos

[![CI](https://github.com/viniciusdepaula25/clouud/actions/workflows/ci.yml/badge.svg)](https://github.com/viniciusdepaula25/clouud/actions/workflows/ci.yml)

Loja de chaves de ativação de jogos (Steam, Epic Games, Ubisoft, Battle.net...), inspirada na Nuuvem e na Green Man Gaming. Tem vitrine com filtros, carrinho, cupons, pagamento simulado com entrega das chaves por e-mail, lista de desejos com avisos, avaliações e um painel administrativo. Começou como trabalho final da escola e está sendo retomado aos poucos.

**Tecnologias:** ASP.NET Core MVC (.NET 10), Entity Framework Core 10, PostgreSQL, Bootstrap 5 (tema Cyborg), testes com pytest + Playwright e CI no GitHub Actions.

## Estrutura

```
.
├── src/Clouud.Web/          # aplicação ASP.NET Core MVC
│   ├── Program.cs           # serviços e pipeline
│   ├── Controllers/         # área pública (início, loja, jogo, conta)
│   ├── Areas/Admin/         # painel administrativo
│   ├── Areas/Cliente/       # área do cliente logado (carrinho, pedidos, chaves, lista de desejos)
│   ├── Models/, ViewModels/ # entidades do banco e modelos das telas
│   ├── Services/            # regras da loja (vitrine, estoque, pedidos, login, e-mails)
│   ├── Infraestrutura/      # dinheiro em reais, criptografia, segurança
│   ├── Data/                # DbContext, admin inicial e migrations
│   ├── Views/               # páginas Razor e e-mails
│   └── wwwroot/             # css, js, imagens e uploads
├── tests/e2e/               # testes automatizados (pytest + Playwright)
├── docs/                    # documentação do projeto
├── Dockerfile, docker-compose.yml
└── .github/                 # CI e Dependabot
```

## Como rodar

### Com Docker (um comando)

```bash
docker compose up --build
```

Abra http://localhost:8080 e entre com `admin@clouud.com` / `Admin@123`. Os e-mails enviados aparecem em http://localhost:8025. Para usar fora do seu computador, copie `.env.example` para `.env` e troque as senhas.

### Sem Docker

Pré-requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download) e PostgreSQL em `localhost:5432`.

1. Guarde as senhas no *user-secrets* (uma vez por computador):
   ```bash
   dotnet user-secrets --project src/Clouud.Web set "Banco:Senha" "postgres"
   dotnet user-secrets --project src/Clouud.Web set "AdminInicial:Senha" "uma-senha-forte"
   dotnet user-secrets --project src/Clouud.Web set "Seguranca:ChaveCriptografia" "$(openssl rand -base64 32)"
   ```
2. Crie o banco:
   ```bash
   dotnet tool restore
   dotnet ef database update --project src/Clouud.Web
   ```
3. Rode a aplicação:
   ```bash
   dotnet run --project src/Clouud.Web
   ```
4. Abra http://localhost:5093 e entre com `admin@clouud.com` e a senha do passo 1.

### Testes

```bash
python3 -m venv .venv && source .venv/bin/activate
pip install -r tests/e2e/requirements.txt
python -m playwright install chromium
dotnet build && python -m pytest tests/e2e
```

## Documentação

- [Funcionalidades](docs/funcionalidades.md): o que cada tela faz (loja, compra, cupons, lista de desejos, painel...)
- [Configuração](docs/configuracao.md): senhas, ajustes, Docker e produção
- [E-mails](docs/emails.md): quais e-mails a loja manda e como configurar o envio
- [Segurança](docs/seguranca.md): o que protege a loja e o que conferir antes de ir para produção
- [Modelo de dados](docs/modelo-de-dados.md): tabelas, decisões e migrations
- [Testes e CI](docs/testes.md): como rodar os testes e o que o GitHub Actions faz
