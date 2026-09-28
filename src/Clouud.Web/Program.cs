using System.Globalization;
using Clouud.Web.Data;
using Clouud.Web.Infraestrutura;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Clouud.Web.Services.Emails;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// HTTPS: fora do desenvolvimento, a loja redireciona HTTP para HTTPS, manda HSTS e os cookies só
// trafegam em HTTPS. Desligue (Seguranca:Https:Forcar=false) só onde não há certificado, como no
// docker compose local. Atrás de um proxy com TLS (nginx, Traefik, Azure...), ligue Seguranca:Proxy:Confiar
// para a aplicação saber o IP e o protocolo originais.
var forcarHttps = builder.Configuration.GetValue("Seguranca:Https:Forcar", !builder.Environment.IsDevelopment());
var confiarProxy = builder.Configuration.GetValue("Seguranca:Proxy:Confiar", false);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false); // não anuncia o servidor
if (confiarProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
    {
        opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // o proxy é quem fala com a aplicação (ela não deve ficar exposta direto na internet)
        opcoes.KnownIPNetworks.Clear();
        opcoes.KnownProxies.Clear();
    });
}
if (forcarHttps)
{
    builder.Services.AddHsts(opcoes =>
    {
        opcoes.MaxAge = TimeSpan.FromDays(365);
        opcoes.IncludeSubDomains = true;
    });
}
var politicaCookie = forcarHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Campos de valor aceitam "59,90" e "59.90" (ver Infraestrutura/Dinheiro.cs)
    options.ModelBinderProviders.Insert(0, new ModeloDecimalBrasileiroProvider());
    // Todo POST exige o token antiforgery, mesmo se alguém esquecer o [ValidateAntiForgeryToken]
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "clouud.antiforgery";
    options.Cookie.SecurePolicy = politicaCookie;
});

// Banco de dados: uma instância do BancoDados por requisição, injetada nos controllers.
// A senha não fica no appsettings: vem de "Banco:Senha" (variável de ambiente Banco__Senha ou user-secrets)
// ou da connection string inteira em ConnectionStrings__LojaJogos.
var conexaoBanco = Segredos.MontarConexaoBanco(builder.Configuration);
builder.Services.AddDbContext<BancoDados>(options => options.UseNpgsql(conexaoBanco));

// Hash das senhas dos usuários
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<SenhaService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<CarrinhoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ListaDesejosService>();
builder.Services.AddScoped<FotoPerfilService>();
builder.Services.AddScoped<PainelService>();
builder.Services.AddScoped<CupomService>();
builder.Services.AddScoped<GaleriaService>();
builder.Services.AddHostedService<CancelamentoAutomatico>();

// E-mails: fila no banco + envio em segundo plano (pasta de arquivos .eml ou SMTP; seção "Email")
builder.Services.Configure<ConfiguracaoEmail>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<LinksLoja>();
builder.Services.AddScoped<RenderizadorEmail>();
builder.Services.AddScoped<FilaEmails>();
builder.Services.AddScoped<ConfirmacaoEmail>();
builder.Services.AddScoped<RedefinicaoSenhaService>();
builder.Services.AddScoped<EmailPedidoPago>();
builder.Services.AddScoped<DescadastroAvisos>();
builder.Services.AddScoped<ConferenciaListaDesejos>();
builder.Services.AddHostedService<AvisosListaDesejos>();
builder.Services.AddHostedService<EnvioEmails>();
builder.Services.AddHttpContextAccessor();


// Adiciona o servico de autenticacao de usuarios por cookies
builder.Services
    .AddAuthentication(options =>
    {
        //opção por cookies
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/conta/login";
        options.LogoutPath = "/conta/logout";
        options.AccessDeniedPath = "/Conta/AcessoNegado";
        options.Cookie.Name = "clouud.sessao";
        options.Cookie.HttpOnly = true;                 // JavaScript não lê o cookie (protege contra XSS)
        options.Cookie.SameSite = SameSiteMode.Lax;     // não vai em POSTs vindos de outros sites
        options.Cookie.SecurePolicy = politicaCookie;    // com HTTPS forçado, só trafega por HTTPS
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        // Cada requisição confere o selo de segurança do usuário: nova senha, novo perfil ou conta
        // excluída derrubam as sessões abertas em outros aparelhos
        options.Events.OnValidatePrincipal = AutenticacaoService.ValidarSessaoAsync;
    });


// Chaves que assinam os cookies de login e os links dos e-mails: nome fixo da aplicação,
// para continuarem valendo depois de mudar a pasta de instalação ou a versão
builder.Services.AddDataProtection().SetApplicationName("Clouud");

// Proteções do login e dos formulários que um robô atacaria
LimitesDeUso.Configurar(builder.Services, builder.Configuration);
builder.Services.AddSingleton<ProtecaoLogin>();

// Criptografia dos códigos das chaves (a chave mestra vem de Seguranca__ChaveCriptografia, nunca do appsettings)
builder.Services.AddSingleton(_ => CriptografiaChaves.Atual);

var app = builder.Build();

CriptografiaChaves.Atual = CriptografiaChaves.DaConfiguracao(app.Configuration);

Segredos.ConferirAoIniciar(app.Configuration, app.Environment, app.Logger);

// Dinheiro no padrão brasileiro: "R$ 1.234,50" (ToString("C") e [DataType(Currency)]).
// Os demais números continuam com ponto (59.90), que é o que o HTML, o SVG e o JavaScript esperam.
var cultura = Dinheiro.CriarCultura();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});

// Configure the HTTP request pipeline.
if (confiarProxy)
{
    app.UseForwardedHeaders(); // primeiro de tudo: IP e protocolo reais vindos do proxy
}
app.UseMiddleware<CabecalhosSeguranca>(forcarHttps);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
if (forcarHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/erro/{0}");
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

//ativa o serviço de autenticacao de usuarios no servidor
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Opcional (Docker e testes automatizados): aplica as migrations pendentes ao iniciar.
// No dia a dia continua valendo "dotnet ef database update"; a opção vem desligada.
if (app.Configuration.GetValue("Banco:AplicarMigrations", false))
{
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<BancoDados>().Database.MigrateAsync();
}

// Contas antigas com senha em texto puro: grava o hash no lugar
await SenhaService.ConverterSenhasLegadasAsync(app.Services);

// Bancos de antes da criptografia: cifra as chaves que ainda estiverem em texto puro
await ConversaoChavesLegadas.ConverterAsync(app.Services);

// Cria o primeiro administrador, se ainda não existir (seção "AdminInicial" do appsettings)
await AdminInicial.CriarAsync(app.Services);

app.Run();
