# Funcionalidades

O que a loja faz, tela por tela. As regras de segurança por trás de cada uma estão em [seguranca.md](seguranca.md) e as tabelas do banco em [modelo-de-dados.md](modelo-de-dados.md).

## Contas e primeiro acesso

O cadastro pelo site cria apenas contas de **cliente**. Ao iniciar, a aplicação cria um **administrador** se ainda não existir nenhum: o e-mail vem de `AdminInicial:Email` (em desenvolvimento, `admin@clouud.com`, no `appsettings.Development.json`) e a senha de `AdminInicial:Senha` (veja [configuracao.md](configuracao.md)). Sem a senha configurada, o log avisa e o admin não é criado.

As senhas precisam ter pelo menos 8 caracteres e não podem estar entre as mais comuns ("12345678", "senha123"...). No banco fica só o *hash* (PBKDF2). Contas do projeto original, com a senha em texto puro, têm a senha trocada pelo hash na primeira vez que a aplicação inicia.

Em **Minha conta** o cliente muda os dados, a senha e a foto de perfil, vê se o e-mail está confirmado, liga ou desliga os avisos da lista de desejos e pode "Sair de todos os outros aparelhos". O login tem "Esqueci minha senha" (veja [emails.md](emails.md)).

## Dinheiro

Valores aparecem no padrão brasileiro (`R$ 1.234,50`). Nos campos de preço e de cupom dá para digitar com vírgula ou ponto: `59,90`, `59.90`, `1.234,50` e `R$ 1.234,50` valem; um ponto seguido de três dígitos é milhar (`1.500` = mil e quinhentos). A regra fica em `Infraestrutura/Dinheiro.cs` e vale no navegador e no servidor.

## Catálogo

- **Jogo:** informações do jogo (título, descrição, capa, lançamento, classificação, desenvolvedora, publicadora e categorias).
- **Produto:** o que a loja vende, ou seja, um jogo em uma plataforma e edição, com preço e preço promocional opcional (com data de término). O jogo só aparece na vitrine depois de ter um produto ativo.
- **Plataformas** e **categorias** são cadastradas no admin e viram os filtros da vitrine.
- **Chaves:** o estoque de cada produto. Produto sem chave disponível aparece como **Esgotado** na vitrine. Uma chave é única em toda a loja, e chaves vendidas não podem ser excluídas. No banco os códigos ficam cifrados; na tela do admin aparecem mascarados, e "Mostrar códigos" abre só os das chaves que nunca foram para um cliente (o acesso fica no log).

No admin, o fluxo é: cadastrar o jogo → cadastrar um ou mais produtos (Steam, Epic Games...) com o preço → importar as chaves de cada produto em **Estoque** (colando uma por linha). Jogos e produtos que já tiveram vendas não podem ser excluídos; desmarque **Ativo** para tirá-los da loja.

## Página inicial

A página inicial segue o modelo das lojas de chaves: no topo, um carrossel com os jogos marcados como **Destaque** no admin; depois, atalhos por plataforma e as prateleiras **Promoções** (maiores descontos), **Mais vendidos** e **Lançamentos**, cada uma com quatro jogos e um link para ver tudo na loja já filtrado. Só aparecem produtos com chave disponível, e cada jogo entra uma vez por prateleira.

## Loja

A loja fica em `/loja`, a mesma para visitante e cliente: busca pelo nome, filtros de plataforma, categoria, faixa de preço (sobre o preço de agora, já com a promoção) e "só promoções", e ordenação por destaques, menor ou maior preço, lançamentos (data de lançamento) ou mais vendidos (unidades em pedidos pagos). Mostra 24 produtos por página; os esgotados ficam sempre no fim. Tudo vai na URL (ex.: `/loja?categoria=rpg&ordem=menor-preco&pagina=2`), então dá para compartilhar uma busca.

## Compra

1. O cliente clica em **Comprar** na vitrine e o produto vai para o **carrinho** (até 10 unidades por produto, limitado ao estoque).
2. Em **Finalizar compra**, o pedido é criado com o preço do momento e as chaves ficam **reservadas por 30 minutos** (ajuste em `Loja:MinutosParaPagar`, no `appsettings.json`). Cada cliente pode ter no máximo 2 pedidos esperando pagamento (`Loja:PedidosPendentesPorCliente`).
3. O **pagamento é simulado**: a tela tem um botão para aprovar e outro para recusar, sem pedir dados de pagamento nem cobrar nada. A simulação só vale com `Pagamento:Simulado=true` (ligado em desenvolvimento e no Docker Compose).
4. Com o pagamento aprovado, as chaves aparecem no pedido e em **Minhas chaves**, com o passo a passo de ativação da plataforma, e chegam por e-mail.

Pedidos não pagos no prazo são cancelados automaticamente e as chaves voltam ao estoque. No admin, **Pedidos** mostra todos os pedidos e permite cancelar os que aguardam pagamento e reembolsar os pagos (as chaves reembolsadas ficam inativas).

## Página do jogo e avaliações

Cada jogo tem uma página pública em `/jogo/{slug}` (ex.: `/jogo/elden-ring`), aberta pelo título do card na vitrine: capa, descrição, ficha técnica, onde comprar (plataformas, preços e estoque) e as avaliações. Só quem tem um pedido **pago** com o jogo pode avaliar (nota de 1 a 5 e comentário opcional); cada cliente tem uma avaliação por jogo, que pode editar ou excluir. A média aparece na vitrine, e em **Admin > Avaliações** o admin filtra e exclui avaliações impróprias.

## Galeria de imagens

Em **Admin > Jogos > Galeria** o admin envia até 12 imagens por jogo (várias de uma vez; JPG, PNG, GIF ou WebP de até 5 MB, conferidas pelo conteúdo do arquivo), coloca legendas e muda a ordem. Elas aparecem na página do jogo com miniaturas para navegar. Os arquivos ficam em `wwwroot/uploads/galeria` e são apagados junto com a imagem ou com o jogo. A **capa** do jogo passa pela mesma conferência (até 5 MB) e fica em `wwwroot/uploads/capas`; capas antigas, gravadas direto em `uploads`, continuam funcionando.

## Cupons de desconto

Em **Admin > Cupons** o admin cria cupons de porcentagem ou de valor fixo, com pedido mínimo, datas de validade, limite de usos no total e por cliente. O cliente digita o código no carrinho e vê o desconto na hora; o cupom é conferido de novo ao finalizar. Pedidos cancelados ou reembolsados devolvem o uso do cupom, e um cupom já usado não pode ser excluído (só desativado).

## Lista de desejos

Na vitrine, o **♡** de cada card põe o jogo na lista de desejos do cliente (vale para todas as plataformas em que o jogo é vendido). A página **Lista de desejos** mostra o preço de agora em cada plataforma, as promoções, o que está esgotado e um botão para comprar. Quando um jogo da lista volta ao estoque ou entra em promoção, o cliente recebe um aviso por e-mail (veja [emails.md](emails.md)). No admin, a tela de **Estoque** mostra quantos clientes querem cada jogo, o que ajuda a decidir o que repor.

## Painel do admin

A tela inicial do admin mostra, para os últimos 7, 30 ou 90 dias: faturamento (só pedidos pagos; reembolsados não entram), pedidos pagos, ticket médio e chaves vendidas, cada um comparado com o período anterior, além de um gráfico de faturamento por dia e os produtos mais vendidos. Também mostra o que precisa de atenção agora: pedidos aguardando pagamento, produtos esgotados, o que repor primeiro (pela procura na lista de desejos) e os últimos pedidos.

Outras telas do admin: jogos, produtos, estoque, pedidos, plataformas, categorias, cupons, avaliações, usuários e **E-mails** (o que foi enviado, o que está na fila e o que falhou).
