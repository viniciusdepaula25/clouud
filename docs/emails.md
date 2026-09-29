# E-mails

A loja manda e-mails automáticos. Eles entram numa fila no banco (tabela `emails`) junto com a operação que os gerou e são enviados em segundo plano; se o servidor de e-mail falhar, a loja tenta de novo (até 5 vezes). Depois do envio o conteúdo é apagado, para links de senha e chaves não ficarem guardados. Em **Admin > E-mails** aparecem o que foi enviado, o que está na fila e o que falhou.

## Quais e-mails

- **Boas-vindas** no cadastro, com o link para **confirmar o e-mail** (vale 7 dias). A conta funciona sem confirmar; em **Minha conta** aparece se o e-mail está confirmado e dá para pedir o link de novo. Trocar o e-mail pede nova confirmação e avisa o endereço antigo.
- **Esqueci minha senha**, no login: o cliente informa o e-mail e recebe um link para criar uma senha nova. O link vale 1 hora e uma vez só (usar um link invalida os outros), no banco fica só o hash do código, e a tela responde igual para e-mails com e sem conta (ninguém descobre quem é cliente). São no máximo 3 pedidos por hora por conta. Depois da troca, chega um aviso de "senha alterada".
- **Pedido pago**: quando o pagamento é aprovado, o cliente recebe o resumo da compra, as chaves e o passo a passo de ativação de cada plataforma. O e-mail é gravado na mesma transação do pagamento; se algo der errado ao montá-lo, o pagamento continua valendo e as chaves seguem em **Minhas chaves**. As chaves só vão para endereço confirmado.
- **Lista de desejos**: quando um jogo da lista **volta ao estoque** ou **entra em promoção** (ou a promoção fica mais barata), o cliente recebe um aviso com os preços de cada plataforma. Um serviço confere as listas a cada minuto (`Loja:AvisosIntervaloSegundos`) e compara com a situação da conferência anterior, então pega todas as causas: chaves importadas ou reativadas, pedido cancelado que devolveu chaves, promoção criada no admin. Só vai para quem confirmou o e-mail e não desligou os avisos; cada aviso tem o link "parar de receber", e a opção também fica em **Minha conta**.

## Onde os e-mails vão parar

Seção `Email` do `appsettings.json`:

| Onde | Configuração | Como ver |
|---|---|---|
| Rodando com `dotnet run` | `"Modo": "Pasta"` (padrão) | arquivos `.eml` em `src/Clouud.Web/emails-enviados/` (abrem no Thunderbird, Outlook ou no navegador) |
| Docker Compose | já vem com o **Mailpit** | caixa de entrada de teste em http://localhost:8025 |
| Envio de verdade | `"Modo": "Smtp"` e os dados em `Smtp` | a caixa do destinatário |

## Envio de verdade (SMTP)

Use um serviço SMTP (Gmail com senha de app, Outlook, Brevo, Mailgun...) e troque o remetente. Não grave a senha no `appsettings.json` (ele vai para o Git): use variáveis de ambiente ou o *user-secrets* do .NET:

```bash
dotnet user-secrets --project src/Clouud.Web set "Email:Modo" "Smtp"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Usuario" "seu.email@gmail.com"
dotnet user-secrets --project src/Clouud.Web set "Email:Smtp:Senha" "senha-de-app"
dotnet user-secrets --project src/Clouud.Web set "Email:Remetente" "seu.email@gmail.com"
```

Os links dos e-mails usam `Loja:UrlPublica` (ex.: `https://clouud.com.br`), e não o endereço que veio na requisição, para ninguém conseguir gerar um link apontando para outro site.
