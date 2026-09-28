# Segurança

Este documento resume o que protege a loja hoje, o que conferir antes de colocá-la no ar e o que ainda pode ser reforçado. Cada item protegido tem teste automatizado em `tests/e2e` (principalmente `test_criptografia.py`, `test_protecao_login.py`, `test_sessao.py`, `test_https.py`, `test_reforcos.py` e `test_senha.py`).

## O que já está protegido

### Dados guardados

| O quê | Como | Onde |
|---|---|---|
| Códigos das chaves de ativação | Cifrados com AES-256-GCM; um HMAC-SHA256 ao lado garante que não se repetem, sem decifrar nada. Bancos antigos são convertidos ao iniciar | `Infraestrutura/CriptografiaChaves.cs`, `ConversaoChavesLegadas.cs` |
| Senhas | Hash PBKDF2 do ASP.NET; senhas antigas em texto puro viram hash ao iniciar e o login nunca compara texto puro | `Services/SenhaService.cs` |
| Links de "esqueci minha senha" | No banco fica só o SHA-256 do código; vale 1 hora e uma vez só | `Services/Emails/RedefinicaoSenhaService.cs` |
| Conteúdo dos e-mails | Apagado depois do envio (e quando a loja desiste de enviar), para links e chaves não ficarem na tabela `emails` | `Services/Emails/EnvioEmails.cs` |
| Segredos (senha do banco, do admin, chave de criptografia, SMTP) | Nunca nos `appsettings*.json`: variáveis de ambiente ou *user-secrets* | `Infraestrutura/Segredos.cs`, README |

### Contas e login

- **Senhas:** mínimo de 8 caracteres, sem as mais comuns (`Infraestrutura/SenhaForteAttribute.cs`).
- **Bloqueio da conta:** 5 senhas erradas seguidas deixam a conta 15 minutos sem entrar (`Services/ProtecaoLogin.cs`). O "esqueci minha senha" desbloqueia.
- **Limites de uso** (rate limiter do ASP.NET, `Infraestrutura/LimitesDeUso.cs`), com resposta 429 e `Retry-After`:
  - login: 10 por minuto por IP;
  - cadastro, "esqueci minha senha" e reenvio de confirmação: 10 a cada 15 minutos por IP;
  - cupom e finalização da compra: 30 por minuto por cliente.
- **Sem descobrir quem é cliente:** o login com e-mail inexistente leva o mesmo tempo, e o "esqueci minha senha" responde igual e demora o mesmo com ou sem conta.
- **Sessão:**
  - o cookie é `HttpOnly`, `SameSite=Lax` e `Secure` com HTTPS, e expira após 30 minutos sem uso;
  - um selo de segurança conferido a cada requisição derruba as sessões abertas quando a senha, o e-mail ou o perfil mudam ou a conta é excluída;
  - Minha conta tem "Sair de todos os outros aparelhos".
- **Logout:** só por POST com token antiforgery.
- **CSRF:** todo POST exige o token antiforgery, por padrão (filtro global).
- **Avisos por e-mail:** troca de senha e troca de e-mail avisam o endereço antigo.
- **Chaves por e-mail:** só vão para endereço confirmado.

### Navegador e transporte

- **HTTPS:** fora do desenvolvimento, HTTP redireciona para HTTPS e a resposta leva HSTS de 1 ano. Atrás de um proxy, `Seguranca:Proxy:Confiar` faz a aplicação ler o IP e o protocolo originais.
- **Cabeçalhos** (`Infraestrutura/CabecalhosSeguranca.cs`):
  - `Content-Security-Policy` com *nonce* por resposta: nenhum script injetado executa;
  - `X-Frame-Options: DENY` e `frame-ancestors 'none'`;
  - `X-Content-Type-Options: nosniff`;
  - `Referrer-Policy`, `Permissions-Policy` e `Cross-Origin-Opener-Policy`;
  - o servidor não se identifica.
- **Sem JavaScript inline:** as views não usam `onclick`, `onerror` nem `onsubmit`; um teste falha se aparecer algum.
- **Cache:** páginas com chaves mandam `Cache-Control: no-store` e não ficam no cache do navegador.
- **Erros:** as páginas de erro não mostram detalhes técnicos fora do desenvolvimento.

### Regras da loja

- **Pagamento:** o pagamento simulado só vale com `Pagamento:Simulado=true` (ligado em desenvolvimento e no docker compose de demonstração). Em produção, sem operadora de pagamento, ninguém "aprova" o próprio pedido.
- **Pedidos abertos:** cada cliente tem no máximo 2 pedidos esperando pagamento, então ninguém trava o estoque com reservas.
- **Chaves na compra:**
  - a reserva usa `FOR UPDATE SKIP LOCKED`;
  - o pagamento confere se todas as chaves do pedido continuam reservadas;
  - inativar ou excluir uma chave no admin é um `UPDATE`/`DELETE` condicional, que não passa por cima de uma compra.
- **Cupons:**
  - o uso é conferido com a linha do cupom travada;
  - cupom de campanha futura responde como inexistente;
  - há limite de tentativas.
- **Admin:**
  - vê os códigos das chaves mascarados. "Mostrar códigos" abre só as que nunca foram para um cliente e fica registrado no log;
  - não consegue rebaixar nem excluir a si mesmo nem o último admin.
- **Formulários:** os do admin só aceitam os campos da tela (sem *overposting*), e os uploads são conferidos pelo conteúdo do arquivo (assinatura), não pela extensão.

## Antes de colocar no ar

1. Gere e guarde a chave de criptografia:
   - gere com `openssl rand -base64 32` e configure em `Seguranca__ChaveCriptografia`;
   - **guarde uma cópia fora do servidor**: sem ela, as chaves do estoque e as já vendidas não podem ser lidas.
2. Defina `Banco__Senha`, `AdminInicial__Senha` e, se usar SMTP, `Email__Smtp__Senha` por variável de ambiente ou pelo cofre de segredos do provedor.
3. Coloque um proxy com certificado na frente (nginx, Traefik, Caddy ou o do provedor) e configure:
   - `Seguranca__Https__Forcar=true`;
   - `Seguranca__Proxy__Confiar=true`;
   - `AllowedHosts=seu.dominio`;
   - `Loja__UrlPublica=https://seu.dominio`.
   Não exponha a porta 8080 da aplicação direto na internet.
4. Mantenha `Pagamento__Simulado=false` até integrar uma operadora de pagamento de verdade.
5. Não publique o Mailpit. Ele é só uma caixa de teste e no `docker-compose.yml` escuta apenas em `127.0.0.1`.
6. Guarde as chaves do Data Protection num volume persistente e com backup (no Docker, o volume `chaves-login`). Se elas se perderem, todos precisam entrar de novo e os links de e-mail pendentes deixam de valer.
7. Faça backup do banco, criptografado, e teste a restauração junto com a chave de criptografia.

## O que ainda pode ser reforçado

| Ponto | Por quê | Sugestão |
|---|---|---|
| Pagamento real | Hoje só existe a simulação | Integrar uma operadora (Mercado Pago, Stripe, PagSeguro) e confirmar o pagamento pelo webhook assinado da operadora, nunca pelo navegador do cliente |
| Imagens enviadas | O arquivo é conferido pela assinatura, mas é gravado como veio (com metadados EXIF, inclusive GPS de fotos de celular) | Recodificar com ImageSharp ou SkiaSharp ao salvar: remove metadados e qualquer conteúdo escondido |
| Chaves do Data Protection | Ficam em arquivos sem criptografia; quem ler o volume consegue forjar cookies | `ProtectKeysWithCertificate` ou um cofre (Azure Key Vault, AWS KMS) |
| Usuário do banco | A aplicação conecta como `postgres` (superusuário) | Criar um usuário só com `SELECT/INSERT/UPDATE/DELETE` para a aplicação e rodar as migrations com outro (ver abaixo) |
| Robôs no cadastro | O limite por IP segura um robô, mas não uma rede de IPs | Captcha (Cloudflare Turnstile ou hCaptcha) no cadastro e no "esqueci minha senha" |
| Mensagem "já existe uma conta com este e-mail" | Revela se um e-mail é cliente | Resposta neutra ("se o e-mail estiver livre, enviamos a confirmação") e aviso por e-mail ao dono |
| Login do admin | Só senha | Segundo fator (TOTP, com app autenticador) para contas de admin |
| Auditoria | Só o "Mostrar códigos" vai para o log | Registrar em tabela quem reembolsou, cancelou, excluiu ou mudou perfis |
| Avaliações | Continuam publicadas depois de um reembolso | Esconder a avaliação quando o cliente não tiver mais pedido pago com o jogo |
| Troca da chave de criptografia | Não há rotina pronta | O formato `v1:` já permite: criar `v2` com a chave nova, decifrar com a antiga e cifrar com a nova num serviço como o `ConversaoChavesLegadas` |
| Versões fixas | O Mailpit usa `latest` e as actions do CI usam tag | Fixar versões (e as actions por SHA). O Dependabot (`.github/dependabot.yml`) avisa das atualizações |

### Usuário do banco com menos privilégios (exemplo)

```sql
-- rode como postgres, uma vez
CREATE ROLE clouud_app LOGIN PASSWORD 'troque';
GRANT CONNECT ON DATABASE lojajogos TO clouud_app;
GRANT USAGE ON SCHEMA public TO clouud_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO clouud_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO clouud_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO clouud_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO clouud_app;
```

A aplicação passa a usar `Username=clouud_app` com `Banco__AplicarMigrations=false`, e as migrations rodam no deploy com o usuário dono do banco (`dotnet ef database update`).
