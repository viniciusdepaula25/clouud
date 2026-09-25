# CLOUUD — Loja de chaves de jogos

Projeto de uma loja de chaves de ativação de jogos (Steam, Epic Games, Ubisoft, Battle.net...), inspirado na Nuuvem e na Green Man Gaming. Começou como trabalho final da escola e está sendo retomado aos poucos.

## Tecnologias

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10
- PostgreSQL
- Bootstrap 5 (tema Bootswatch Cyborg)

## Como rodar

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

- **Jogo:** informações do jogo (título, descrição, capa, lançamento, classificação, desenvolvedora, publicadora e categorias).
- **Produto:** o que a loja vende, ou seja, um jogo em uma plataforma e edição, com preço e preço promocional opcional (com data de término). O jogo só aparece na vitrine depois de ter um produto ativo.
- **Plataformas** e **categorias** são cadastradas no admin e viram os filtros da vitrine.
- **Chaves:** o estoque de cada produto. Produto sem chave disponível aparece como **Esgotado** na vitrine. Uma chave é única em toda a loja, e chaves vendidas não podem ser excluídas.

No admin, o fluxo é: cadastrar o jogo → cadastrar um ou mais produtos (Steam, Epic Games...) com o preço → importar as chaves de cada produto em **Estoque** (colando uma por linha). Jogos e produtos que já tiveram vendas não podem ser excluídos; desmarque **Ativo** para tirá-los da loja.

## Estrutura

```
.
├── Clouud.sln
├── global.json                 # versão do SDK .NET usada no projeto
├── .config/dotnet-tools.json   # versão do dotnet-ef usada no projeto
├── .editorconfig               # padrão de formatação (UTF-8, LF, indentação)
└── src/
    └── Clouud.Web/             # aplicação ASP.NET Core MVC
        ├── Program.cs          # configuração da aplicação (serviços e pipeline)
        ├── Controllers/        # controllers da área pública (Home, Conta)
        ├── Areas/
        │   ├── Admin/          # painel administrativo (jogos, produtos, estoque, plataformas, categorias, usuários)
        │   └── Cliente/        # área do cliente logado
        ├── Models/             # entidades do banco (Usuario, Jogo, Produto, Chave, Plataforma, Categoria, Empresa, Pedido...)
        ├── ViewModels/         # modelos das telas (vitrine, formulários, login, cadastro)
        ├── Services/           # regras reutilizáveis (hash de senha, login, vitrine, estoque, slugs)
        ├── Data/
        │   ├── BancoDados.cs   # DbContext do Entity Framework
        │   ├── AdminInicial.cs # cria o primeiro administrador
        │   └── Migrations/     # histórico de alterações do banco
        ├── Views/              # páginas Razor da área pública
        └── wwwroot/
            ├── css/, js/       # estilos e scripts do site
            ├── img/            # imagens fixas do layout
            ├── lib/            # bibliotecas de terceiros (Bootstrap, jQuery)
            └── uploads/        # imagens enviadas pelo admin (fora do Git)
```

## Banco de dados

Depois de alterar alguma classe em `Models/`, gere uma nova migration e aplique:

```bash
dotnet ef migrations add NomeDaAlteracao --project src/Clouud.Web --output-dir Data/Migrations
dotnet ef database update --project src/Clouud.Web
```
