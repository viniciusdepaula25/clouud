# Configuração

Como a aplicação recebe senhas e ajustes, o que o Docker Compose faz e o que configurar em produção. A lista completa do que conferir antes de colocar a loja no ar está em [seguranca.md](seguranca.md#antes-de-colocar-no-ar).

## Senhas e configurações secretas

Nenhuma senha fica nos arquivos `appsettings*.json`. Cada uma vem de uma variável de ambiente (em produção, no Docker e no CI) ou do *user-secrets* do .NET (no desenvolvimento, guardado na pasta do usuário, fora do projeto e do Git). No nome da variável, os dois-pontos viram dois sublinhados:

| Configuração | Variável de ambiente | Para que serve |
|---|---|---|
| `Banco:Senha` | `Banco__Senha` | Senha do PostgreSQL (ou passe a connection string inteira em `ConnectionStrings__LojaJogos`) |
| `AdminInicial:Senha` | `AdminInicial__Senha` | Senha do primeiro administrador, criado só se ainda não existir nenhum (mínimo de 8 caracteres) |
| `Seguranca:ChaveCriptografia` | `Seguranca__ChaveCriptografia` | Chave (32 bytes em Base64) que cifra os códigos das chaves de ativação no banco. Obrigatória: sem ela a aplicação não inicia |
| `Email:Smtp:Senha` | `Email__Smtp__Senha` | Senha do servidor de e-mail, se usar SMTP (veja [emails.md](emails.md)) |

Servidor, banco e usuário do PostgreSQL ficam em `src/Clouud.Web/appsettings.json` (`ConnectionStrings:LojaJogos`), sem a senha.

**Guarde uma cópia da `ChaveCriptografia`** (gerenciador de senhas, cofre do provedor de nuvem). Sem ela, os códigos das chaves do estoque e das já vendidas não podem mais ser lidos, nem restaurando um backup do banco. Gere uma com `openssl rand -base64 32`.

## Outros ajustes (`appsettings.json`)

| Seção | O que ajusta |
|---|---|
| `Loja:MinutosParaPagar` | Tempo que as chaves ficam reservadas esperando o pagamento (30) |
| `Loja:PedidosPendentesPorCliente` | Pedidos esperando pagamento ao mesmo tempo por cliente (2) |
| `Loja:AvisosIntervaloSegundos` | De quanto em quanto tempo as listas de desejos são conferidas (60) |
| `Loja:UrlPublica` | Endereço usado nos links dos e-mails |
| `Pagamento:Simulado` | Liga os botões de aprovar/recusar o pagamento (padrão: só em desenvolvimento) |
| `Email` | Onde os e-mails vão parar (veja [emails.md](emails.md)) |
| `Seguranca` | Limites de tentativas de login, cadastro, cupom e compra, HTTPS e proxy |

## Docker Compose

`docker compose up --build` sobe a aplicação em http://localhost:8080, um PostgreSQL 17 próprio (não usa a porta 5432 do seu computador) e o Mailpit (http://localhost:8025, só em `127.0.0.1`). Cria as tabelas e o primeiro administrador (`admin@clouud.com` / `Admin@123`).

- As senhas de exemplo do `docker-compose.yml` servem só para testar no seu computador. Para qualquer outro uso, copie `.env.example` para `.env` e troque os valores (o `.env` não vai para o Git).
- Os dados do banco, as imagens enviadas e as chaves dos cookies de login ficam em volumes do Docker, então sobrevivem a `docker compose down`. Para apagar tudo e começar do zero: `docker compose down -v`.
- Na primeira vez o log mostra um erro de `__EFMigrationsHistory` não existir: é o Entity Framework conferindo o banco vazio antes de criar as tabelas.
- No Compose local, `Seguranca__Https__Forcar` fica `false` (não há certificado) e `Pagamento__Simulado` fica `true`.

## Produção

A loja deve ficar atrás de um proxy com o certificado (nginx, Traefik, Caddy, o do provedor de nuvem). Configure:

| Configuração | Para quê |
|---|---|
| `Seguranca__Https__Forcar=true` | Redireciona HTTP para HTTPS e manda HSTS. É o padrão fora do desenvolvimento |
| `Seguranca__Proxy__Confiar=true` | A aplicação lê o IP e o protocolo originais dos cabeçalhos `X-Forwarded-*` (só ligue atrás de um proxy: exposta direto, qualquer um falsificaria o IP) |
| `AllowedHosts=clouud.com.br` | Só responde ao domínio da loja |
| `Loja__UrlPublica=https://clouud.com.br` | Links dos e-mails em HTTPS |
| `Pagamento__Simulado=false` | Até integrar uma operadora de pagamento de verdade |
