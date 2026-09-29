# Guia visual

A identidade do CLOUUD é escura e "gamer", com neon vermelho: fundo preto com os tubos vermelhos (`img/fundopv.jpg`), barra vermelha no topo e o vermelho usado como acento (selos, botão de compra, títulos de seção). As telas usam Bootstrap 5.3 com o tema Bootswatch Cyborg (`css/mytheme.css`), mais duas folhas próprias:

| Arquivo | O que tem |
|---|---|
| `wwwroot/css/tema.css` | **Tokens**: cores, fontes, raios, sombras e espaçamentos, e as variáveis do Bootstrap apontadas para eles. É o único lugar com valores de cor |
| `wwwroot/css/site.css` | **Componentes** da loja e do admin, sempre usando os tokens |

Fontes e ícones são servidos pelo próprio site (`wwwroot/lib/fontes` e `wwwroot/lib/bootstrap-icons`), sem Google Fonts nem CDN, então a página fica igual em qualquer computador e a CSP não precisa liberar outros domínios.

## Tokens

### Cores

| Token | Valor | Uso |
|---|---|---|
| `--cl-fundo` | `#060606` | fundo da página (atrás da imagem) |
| `--cl-superficie` | `#151515` | cards |
| `--cl-superficie-2` | `#1d1d1d` | cabeçalho de tabela, blocos internos |
| `--cl-superficie-3` | `#282828` | hover, destaque suave |
| `--cl-painel` | `rgba(14,14,14,.9)` | painel sobre a imagem de fundo |
| `--cl-borda` / `--cl-borda-forte` | `#2c2c2c` / `#444` | bordas, campos |
| `--cl-texto` | `#b9bbba` | texto padrão (9,5:1) |
| `--cl-titulo` | `#fff` | títulos |
| `--cl-texto-suave` | `#8e9190` | texto secundário (5,7:1) |
| `--cl-vermelho` | `#cc0000` | barra, selos de desconto, botão Comprar, paginação ativa |
| `--cl-vermelho-texto` | `#ff5252` | vermelho em texto pequeno e erros de validação (5,7:1) |
| `--cl-azul` / `--cl-azul-botao` / `--cl-azul-texto` | `#2a9fd6` / `#1d78a8` / `#5cb8e4` | gráfico e foco / botão principal de formulário (4,9:1 com texto branco) / links |
| `--cl-laranja` | `#ff8800` | estrelas, avisos |
| `--cl-verde` / `--cl-verde-texto` | `#77b300` / `#9bd11f` | sucesso, "à venda" |
| `--cl-roxo` / `--cl-roxo-texto` | `#9933cc` / `#c77dff` | informação, reembolsado |

Os números entre parênteses são o contraste com a superfície (`#151515`); todos passam no AA.

### Tipografia

- **Roboto** 400/500/700 no corpo (`--cl-fonte`).
- **Anton** (condensada e pesada, no lugar da Impact) na marca "CLOUUD", nos títulos de seção, no título do destaque e no nome do jogo (`--cl-fonte-marca`). Os textos ficam na caixa original: nada de `text-transform: uppercase` em título, para o texto copiado e lido pelo leitor de tela ser o mesmo.
- Rótulos pequenos (cabeçalho de tabela, legendas de formulário, indicadores do painel) usam caixa alta com espaçamento.

### Forma e espaço

`--cl-raio` (0,75rem), `--cl-raio-sm`, `--cl-raio-lg`, `--cl-sombra`, `--cl-sombra-sm`, `--cl-neon` (brilho vermelho do card em foco), `--cl-foco` (anel azul do teclado), `--cl-espaco-1` a `--cl-espaco-5`, `--cl-espaco-secao` (3rem entre seções) e `--cl-toque` (44 px, área mínima de toque).

## Componentes (`site.css`)

| Componente | Classes | Onde aparece |
|---|---|---|
| Barra | `.barra-loja`, `.barra-admin`, `.busca-barra`, `.secoes-barra`, `.acoes-barra` | topo. No desktop, marca, busca e conta em cima e as seções embaixo; no celular, tudo no menu ☰ |
| Rodapé | `.rodape` | loja e admin |
| Painel | `.painel`, `.painel-titulo`, `.painel-form` | blocos de conteúdo sobre o fundo; `.painel-form` nas telas de conta |
| Cabeçalho de página | `.cabecalho-pagina`, `.migalhas`, `.subtitulo`, `.acoes` | título de toda tela interna, com as ações à direita |
| Título de seção | `.titulo-secao`, `.subtitulo-secao`, `.ver-todos` | prateleiras, página do jogo, painel |
| Card de produto | `.card-produto`, `.capa-produto`, `.selo-desconto`, `.selo-esgotado`, `.botao-desejo`, `.preco` | loja, página inicial |
| Chips | `.chip-plataforma`, `.chip-categoria` | atalhos da página inicial, categorias do jogo |
| Filtros | `.filtros-loja`, `.barra-resultados`, `.filtros-admin` | loja e listas do admin |
| Tabela | `.tabela-painel` + `.table-responsive`, `.acoes-linha`, `.capa-tabela` | listas; no celular a tabela rola por dentro, nunca a página |
| Situação | `.situacao` + `-sucesso`, `-aviso`, `-perigo`, `-info`, `-neutra`, `-roxa` | pedidos, cupons, chaves, produtos (bolinha + texto: a cor nunca é o único sinal) |
| Indicador | `.indicador` (`--cor-indicador`), `.filtro-situacao` | painel do admin, resumo dos pedidos |
| Formulário | `.form-admin`, `.rodape-form`, `.grupo-form`, `.grade-opcoes`, `.capa-previa` | cadastros do admin e Minha conta |
| Chave | `.chave-entregue`, `.como-ativar` | pedido pago e Minhas chaves |
| Compra | `.item-carrinho`, `.resumo-compra`, `.linha-resumo`, `.cupom-aplicado`, `.metodos-pagamento`, `.aviso-simulacao` | carrinho e pagamento |
| Estado vazio | `.estado-vazio` | listas sem itens, sempre com uma ação |
| Alerta | `.alert-*`, `.alert-com-icone` | mensagens depois de salvar/excluir (`Views/Shared/_Alertas.cshtml`) |

### Estados

- **Sucesso e erro**: alerta escuro com borda colorida à esquerda e ícone.
- **Validação**: texto vermelho claro embaixo do campo e a borda do campo acende.
- **Vazio**: ícone, frase, dica e botão ("Ver jogos").
- **Desabilitado**: botão com 55% de opacidade (Esgotado, mover a primeira imagem para a esquerda, galeria cheia).
- **Confirmação**: continua sendo o `confirm()` do navegador, pelo atributo `data-confirmar` (tratado no `site.js`).

## Regras

- **Contraste AA** em todo texto; o foco do teclado aparece sempre (anel azul).
- **Celular**: nada rola para o lado. Botões, campos e links de ação têm pelo menos 44 px nas telas de toque, e os atalhos de plataforma viram uma faixa que rola.
- **Movimento**: só transições curtas em CSS; com "reduzir movimento" ligado, os cards não se mexem.
- **Sem JavaScript inline** (CSP): comportamentos pelos atributos `data-*` do `site.js`.
- **Cores só nos tokens**: um componente novo usa as variáveis de `tema.css`, não valores soltos.

## O que mudou em cada tela

| Tela | Mudança |
|---|---|
| Barra e rodapé | Barra com degradê vermelho, logo em branco, busca larga com lupa, ícones nos links e duas linhas no desktop. Rodapé com a marca, links da loja e da conta e o aviso do pagamento simulado |
| Página inicial | Destaque maior com título na fonte da marca, preço e botão juntos; atalhos por plataforma em chips; prateleiras com título de seção e "Ver todos" em pílula |
| Loja | Filtros num painel (recolhível no celular), "Só promoções" como chave liga/desliga, contador e ordenação numa barra acima da grade; estado vazio com ação |
| Card de produto | Capa em cima (proporção de capa de loja), selo de desconto e coração sobre a capa, "Esgotado" no meio da capa escurecida, preço e Comprar na base |
| Página do jogo | Topo com a capa desfocada ao fundo; "Onde comprar" e a ficha técnica numa coluna lateral fixa; galeria, descrição e avaliações na coluna principal; estoque ("Entrega imediata", "Últimas chaves") em cada oferta |
| Entrar, criar conta, senha | Caixa central com a logo, botão principal em largura total e link secundário discreto |
| Acesso negado, erro, links | Ícone de situação no topo da caixa |
| Carrinho | Itens em lista com capa e um resumo lateral fixo com cupom, subtotal, desconto, total e Finalizar compra |
| Pagamento | Contagem regressiva em destaque, formas de pagamento como opções grandes, aviso de simulação tracejado |
| Pedido | Chaves em blocos com Copiar e "Como ativar"; itens e pagamentos em painéis |
| Meus pedidos, Minhas chaves | Tabela em painel com selos de situação; chaves agrupadas por pedido |
| Lista de desejos | Card com a capa à esquerda e uma linha por plataforma |
| Minha conta | Foto e aparelhos conectados numa coluna, dados e senha num formulário em seções |
| Admin | Mesma barra (com o selo "Admin"), cabeçalho de página com as ações à direita, tabelas em painel, formulários em seções com os botões no rodapé, exclusão com alerta vermelho, painel com indicadores coloridos |

## Sugestões (não implementadas)

- Trocar o `confirm()` do navegador por uma janela do Bootstrap com o mesmo texto.
- Paginação nas listas do admin (pedidos e avaliações mostram só os mais recentes).
- Fotos reais das plataformas (logos) nos chips e nos cards, se houver licença para usá-las.
