# CLOUUD — Loja de chaves de jogos

[![CI](https://github.com/viniciusdepaula25/clouud/actions/workflows/ci.yml/badge.svg)](https://github.com/viniciusdepaula25/clouud/actions/workflows/ci.yml)

Projeto de uma loja de chaves de ativação de jogos (Steam, Epic Games, Ubisoft, Battle.net...), inspirado na Nuuvem e na Green Man Gaming. Começou como trabalho final da escola e está sendo retomado aos poucos.

## Tecnologias

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10
- PostgreSQL
- Bootstrap 5 (tema Bootswatch Cyborg)

## Rodar com Docker (um comando)

Com o [Docker](https://docs.docker.com/engine/install/) instalado, na pasta do projeto:

```bash
docker compose up --build
```

Abra http://localhost:8080 e entre com `admin@clouud.com` / `Admin@123`. O Compose sobe a aplicação e um PostgreSQL 17 próprio (não usa a porta 5432 do seu computador), cria as tabelas e o primeiro administrador. Os dados do banco, as imagens enviadas e as chaves dos cookies de login ficam em volumes do Docker, então sobrevivem a `docker compose down`; para apagar tudo e começar do zero use `docker compose down -v`.

Na primeira vez o log mostra um erro de `__EFMigrationsHistory` não existir: é o Entity Framework conferindo o banco vazio antes de criar as tabelas.

## Como rodar (sem Docker)

**Pré-requisitos:** [.NET SDK 10](https://dotnet.microsoft.com/download) e um PostgreSQL rodando em `localhost:5432`.

1. Ajuste usuário e senha do banco em `src/Clouud.Web/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "LojaJogos": "Host=localhost;Port=5432;Database=lojajogos;Username=postgres;Password=postgres"
   }
   ```
2. Restaure as ferramentas e crie o banco:
   ```bash
   dotnet tool restore
   dotnet ef database update --project src/Clouud.Web
   ```
3. Rode a aplicação:
   ```bash
   dotnet run --project src/Clouud.Web
   ```
4. Abra http://localhost:5093

### Primeiro acesso

O cadastro pelo site cria apenas contas de **cliente**. Ao iniciar em modo de desenvolvimento, a aplicação cria um **administrador** se ainda não existir nenhum, usando a seção `AdminInicial` do `src/Clouud.Web/appsettings.Development.json`:

| E-mail | Senha |
|---|---|
| `admin@clouud.com` | `Admin@123` |

As senhas são salvas no banco apenas como *hash* (PBKDF2). Contas antigas, com a senha salva em texto puro, continuam funcionando: no primeiro login a senha é convertida para hash automaticamente.

## Catálogo

Valores aparecem no padrão brasileiro (`R$ 1.234,50`). Nos campos de preço e de cupom dá para digitar com vírgula ou ponto: `59,90`, `59.90`, `1.234,50` e `R$ 1.234,50` valem; um ponto seguido de três dígitos é milhar (`1.500` = mil e quinhentos). A regra fica em `Infraestrutura/Dinheiro.cs` e vale no navegador e no servidor.

- **Jogo:** informações do jogo (título, descrição, capa, lançamento, classificação, desenvolvedora, publicadora e categorias).
- **Produto:** o que a loja vende, ou seja, um jogo em uma plataforma e edição, com preço e preço promocional opcional (com data de término). O jogo só aparece na vitrine depois de ter um produto ativo.
- **Plataformas** e **categorias** são cadastradas no admin e viram os filtros da vitrine.
- **Chaves:** o estoque de cada produto. Produto sem chave disponível aparece como **Esgotado** na vitrine. Uma chave é única em toda a loja, e chaves vendidas não podem ser excluídas.

No admin, o fluxo é: cadastrar o jogo → cadastrar um ou mais produtos (Steam, Epic Games...) com o preço → importar as chaves de cada produto em **Estoque** (colando uma por linha). Jogos e produtos que já tiveram vendas não podem ser excluídos; desmarque **Ativo** para tirá-los da loja.

## Página inicial

A página inicial segue o modelo das lojas de chaves: no topo, um carrossel com os jogos marcados como **Destaque** no admin; depois, atalhos por plataforma e as prateleiras **Promoções** (maiores descontos), **Mais vendidos** e **Lançamentos**, cada uma com quatro jogos e um link para ver tudo na loja já filtrado. Só aparecem produtos com chave disponível, e cada jogo entra uma vez por prateleira.

## Loja

A loja fica em `/loja`, a mesma para visitante e cliente: busca pelo nome, filtros de plataforma, categoria, faixa de preço (sobre o preço de agora, já com a promoção) e "só promoções", e ordenação por destaques, menor ou maior preço, lançamentos (data de lançamento) ou mais vendidos (unidades em pedidos pagos). Mostra 24 produtos por página; os esgotados ficam sempre no fim. Tudo vai na URL (ex.: `/loja?categoria=rpg&ordem=menor-preco&pagina=2`), então dá para compartilhar uma busca.

## Compra

1. O cliente clica em **Comprar** na vitrine e o produto vai para o **carrinho** (até 10 unidades por produto, limitado ao estoque).
2. Em **Finalizar compra**, o pedido é criado com o preço do momento e as chaves ficam **reservadas por 30 minutos** (ajuste em `Loja:MinutosParaPagar`, no `appsettings.json`).
3. O **pagamento é simulado**: a tela tem um botão para aprovar e outro para recusar, sem pedir dados de pagamento nem cobrar nada.
4. Com o pagamento aprovado, as chaves aparecem no pedido e em **Minhas chaves**, com o passo a passo de ativação da plataforma.

Pedidos não pagos no prazo são cancelados automaticamente e as chaves voltam ao estoque. No admin, **Pedidos** mostra todos os pedidos e permite cancelar os que aguardam pagamento e reembolsar os pagos (as chaves reembolsadas ficam inativas).

## Página do jogo e avaliações

Cada jogo tem uma página pública em `/jogo/{slug}` (ex.: `/jogo/elden-ring`), aberta pelo título do card na vitrine: capa, descrição, ficha técnica, onde comprar (plataformas, preços e estoque) e as avaliações. Só quem tem um pedido **pago** com o jogo pode avaliar (nota de 1 a 5 e comentário opcional); cada cliente tem uma avaliação por jogo, que pode editar ou excluir. A média aparece na vitrine, e em **Admin > Avaliações** o admin filtra e exclui avaliações impróprias.

## Galeria de imagens

Em **Admin > Jogos > Galeria** o admin envia até 12 imagens por jogo (várias de uma vez; JPG, PNG, GIF ou WebP de até 5 MB, conferidas pelo conteúdo do arquivo), coloca legendas e muda a ordem. Elas aparecem na página do jogo com miniaturas para navegar. Os arquivos ficam em `wwwroot/uploads/galeria` e são apagados junto com a imagem ou com o jogo. A **capa** do jogo passa pela mesma conferência (até 5 MB) e fica em `wwwroot/uploads/capas`; capas antigas, gravadas direto em `uploads`, continuam funcionando.

## Cupons de desconto

Em **Admin > Cupons** o admin cria cupons de porcentagem ou de valor fixo, com pedido mínimo, datas de validade, limite de usos no total e por cliente. O cliente digita o código no carrinho e vê o desconto na hora; o cupom é conferido de novo ao finalizar. Pedidos cancelados ou reembolsados devolvem o uso do cupom, e um cupom já usado não pode ser excluído (só desativado).

## Lista de desejos

Na vitrine, o **♡** de cada card põe o jogo na lista de desejos do cliente (vale para todas as plataformas em que o jogo é vendido). A página **Lista de desejos** mostra o preço de agora em cada plataforma, as promoções, o que está esgotado e um botão para comprar. No admin, a tela de **Estoque** mostra quantos clientes querem cada jogo, o que ajuda a decidir o que repor.

## Painel do admin

A tela inicial do admin mostra, para os últimos 7, 30 ou 90 dias: faturamento (só pedidos pagos; reembolsados não entram), pedidos pagos, ticket médio e chaves vendidas, cada um comparado com o período anterior, além de um gráfico de faturamento por dia e os produtos mais vendidos. Também mostra o que precisa de atenção agora: pedidos aguardando pagamento, produtos esgotados, o que repor primeiro (pela procura na lista de desejos) e os últimos pedidos.

## E-mails

A loja manda e-mails automáticos. Eles entram numa fila no banco (tabela `emails`) junto com a operação que os gerou e são enviados em segundo plano; se o servidor de e-mail falhar, a loja tenta de novo (até 5 vezes). Depois do envio o conteúdo é apagado, para links de senha e chaves não ficarem guardados. Em **Admin > E-mails** aparecem o que foi enviado, o que está na fila e o que falhou.

- **Boas-vindas** no cadastro, com o link para **confirmar o e-mail** (vale 7 dias). A conta funciona sem confirmar; em **Minha conta** aparece se o e-mail está confirmado e dá para pedir o link de novo. Trocar o e-mail pede nova confirmação.
- **Esqueci minha senha**, no login: o cliente informa o e-mail e recebe um link para criar uma senha nova. O link vale 1 hora e uma vez só (usar um link invalida os outros), no banco fica só o hash do código, e a tela responde igual para e-mails com e sem conta (ninguém descobre quem é cliente). São no máximo 3 pedidos por hora por conta. Depois da troca, chega um aviso de "senha alterada".
- **Pedido pago**: quando o pagamento é aprovado, o cliente recebe o resumo da compra, as chaves e o passo a passo de ativação de cada plataforma. O e-mail é gravado na mesma transação do pagamento; se algo der errado ao montá-lo, o pagamento continua valendo e as chaves seguem em **Minhas chaves**.
- **Lista de desejos**: quando um jogo da lista **volta ao estoque** ou **entra em promoção** (ou a promoção fica mais barata), o cliente recebe um aviso com os preços de cada plataforma. Um serviço confere as listas a cada minuto (`Loja:AvisosIntervaloSegundos`) e compara com a situação da conferência anterior, então pega todas as causas: chaves importadas ou reativadas, pedido cancelado que devolveu chaves, promoção criada no admin. Só vai para quem confirmou o e-mail e não desligou os avisos; cada aviso tem o link "parar de receber", e a opção também fica em **Minha conta**.

Onde os e-mails vão parar (seção `Email` do `appsettings.json`):

| Onde | Configuração | Como ver |
|---|---|---|
| Rodando com `dotnet run` | `"Modo": "Pasta"` (padrão) | arquivos `.eml` em `src/Clouud.Web/emails-enviados/` (abrem no Thunderbird, Outlook ou no navegador) |
| Docker Compose | já vem com o **Mailpit** | caixa de entrada de teste em http://localhost:8025 |
| Envio de verdade | `"Modo": "Smtp"` e os dados em `Smtp` | a caixa do destinatário |

Para enviar de verdade, use um serviço SMTP (Gmail com senha de app, Outlook, Brevo, Mailgun...) e troque o remetente. Não grave a senha no `appsettings.json` (ele vai para o Git): use variáveis de ambiente ou o *user-secrets* do .NET, que guarda as configurações só no seu computador e vale no modo de desenvolvimento:

```bash
dotnet user-secrets --project src/Clouud.Web set "Email:Modo" "Smtp"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Usuario" "seu.email@gmail.com"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Senha" "senha-de-app"
dotnet user-secrets --project src/Clouud.Web set "Email:Remetente" "seu.email@gmail.com"
```

Os links dos e-mails usam `Loja:UrlPublica` (ex.: `https://clouud.com.br`), e não o endereço que veio na requisição, para ninguém conseguir gerar um link apontando para outro site.

## Estrutura

```
.
├── Clouud.sln
├── global.json                 # versão do SDK .NET usada no projeto
├── .config/dotnet-tools.json   # versão do dotnet-ef usada no projeto
├── .editorconfig               # padrão de formatação (UTF-8, LF, indentação)
├── Dockerfile, docker-compose.yml  # rodar com Docker
├── .github/workflows/ci.yml    # build e testes a cada push (GitHub Actions)
├── tests/e2e/                  # testes automatizados (pytest + Playwright)
└── src/
    └── Clouud.Web/             # aplicação ASP.NET Core MVC
        ├── Program.cs          # configuração da aplicação (serviços e pipeline)
        ├── Controllers/        # controllers da área pública (Home, Conta)
        ├── Areas/
        │   ├── Admin/          # painel administrativo (jogos, produtos, estoque, pedidos, plataformas, categorias, usuários)
        │   └── Cliente/        # área do cliente logado (loja, carrinho, pedidos, minhas chaves, lista de desejos, minha conta)
        ├── Models/             # entidades do banco (Usuario, Jogo, Produto, Chave, Pedido, Pagamento, CarrinhoItem...)
        ├── ViewModels/         # modelos das telas (vitrine, formulários, login, cadastro)
        ├── Infraestrutura/     # peças do ASP.NET adaptadas à loja (dinheiro em reais)
        ├── Services/           # regras reutilizáveis (login, vitrine, estoque, carrinho, pedidos, lista de desejos, slugs)
        ├── Data/
        │   ├── BancoDados.cs   # DbContext do Entity Framework
        │   ├── NomesSnakeCase.cs # nomes do banco em minúsculo snake_case
        │   ├── AdminInicial.cs # cria o primeiro administrador
        │   └── Migrations/     # histórico de alterações do banco
        ├── Views/              # páginas Razor da área pública; Shared/_Layout.cshtml é o layout único da loja (visitante e cliente)
        └── wwwroot/
            ├── css/, js/       # estilos e scripts do site
            ├── img/            # imagens fixas do layout
            ├── lib/            # bibliotecas de terceiros (Bootstrap, jQuery)
            └── uploads/        # capas/, perfis/ (fotos de perfil) e galeria/ (fora do Git)
```

## Banco de dados

Tabelas e colunas ficam em minúsculo snake_case, então o SQL escrito à mão não precisa de aspas:

```sql
SELECT j.titulo, p.edicao, p.preco FROM produtos p JOIN jogos j ON j.id = p.jogo_id;
```

No C# as classes continuam em PascalCase (`PedidoItem.PrecoUnitario` vira a coluna `pedido_itens.preco_unitario`); a conversão é feita em `Data/NomesSnakeCase.cs`. Check constraints e SQL escrito à mão (`migrationBuilder.Sql`, `FromSql`) devem usar os nomes em snake_case.

Depois de alterar alguma classe em `Models/`, gere uma nova migration e aplique:

```bash
dotnet ef migrations add NomeDaAlteracao --project src/Clouud.Web --output-dir Data/Migrations
dotnet ef database update --project src/Clouud.Web
```

## Testes

Os testes em `tests/e2e` usam a aplicação de verdade: sobem o CLOUUD num banco só para testes (`clouud_testes`, apagado e recriado a cada execução; o `lojajogos` não é tocado) e usam um navegador Chromium (Playwright) para fazer o que um cliente e um admin fazem, conferindo o resultado nas telas e no banco. Cobrem catálogo, vitrine, estoque, compra, cupons, avaliações, lista de desejos, conta e foto de perfil, galeria e capa, painel, as regras do banco e compras simultâneas (a mesma chave nunca é vendida duas vezes).

Na primeira vez (precisa de Python 3.10 ou mais novo):

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r tests/e2e/requirements.txt
python -m playwright install chromium
```

No Ubuntu/Pop!_OS, se o `venv` reclamar, instale antes com `sudo apt install python3-venv`; se o Chromium não abrir por falta de bibliotecas do sistema, rode `python -m playwright install --with-deps chromium`.

Para rodar (com o PostgreSQL ligado em `localhost:5432`, usuário e senha `postgres`):

```bash
source .venv/bin/activate
dotnet build
python -m pytest tests/e2e              # todos (uns 5 minutos)
python -m pytest tests/e2e -m "not lento" # sem os de concorrência e conversão do banco antigo
python -m pytest tests/e2e/test_cupons.py # um arquivo só
```

Outro servidor ou senha do banco: variáveis `CLOUUD_PG_HOST`, `CLOUUD_PG_PORTA`, `CLOUUD_PG_USUARIO` e `CLOUUD_PG_SENHA`. Quando um teste falha, o log da aplicação e capturas de tela das páginas ficam em `tests/e2e/.saida/`.

## Integração contínua

A cada `git push` (e em pull requests) o GitHub Actions (`.github/workflows/ci.yml`) compila o projeto, sobe um PostgreSQL 17 e roda todos os testes. O resultado aparece na aba **Actions** do repositório e no selo no topo deste README; se algo falhar, o log e as capturas de tela ficam disponíveis para baixar na própria execução.
