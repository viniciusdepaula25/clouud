using System.Globalization;
using Clouud.Web.Data;
using Clouud.Web.Infraestrutura;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    // Campos de valor aceitam "59,90" e "59.90" (ver Infraestrutura/Dinheiro.cs)
    options.ModelBinderProviders.Insert(0, new ModeloDecimalBrasileiroProvider()));

// Banco de dados: uma instância do BancoDados por requisição, injetada nos controllers
builder.Services.AddDbContext<BancoDados>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LojaJogos")));

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
        options.LogoutPath = "/";
        options.AccessDeniedPath = "/Conta/AcessoNegado";
    });

// Adiciona o serviço de envio de arquivos
builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));


var app = builder.Build();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

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

// Cria o primeiro administrador, se ainda não existir (seção "AdminInicial" do appsettings)
await AdminInicial.CriarAsync(app.Services);

app.Run();
