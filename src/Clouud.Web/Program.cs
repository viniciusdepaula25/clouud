using System.Globalization;
using Clouud.Web.Data;
using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

// Números sempre com ponto decimal (ex.: 59.90), igual à validação do navegador.
// Sem isso, em um computador com sistema em português "59.90" vira 5990.
// A moeda é o real: valores com [DataType(Currency)] aparecem como "R$ 59.90".
var cultura = (CultureInfo)CultureInfo.InvariantCulture.Clone();
cultura.NumberFormat.CurrencySymbol = "R$";
cultura.NumberFormat.CurrencyPositivePattern = 2; // "R$ 59.90"
cultura.NumberFormat.CurrencyNegativePattern = 9; // "-R$ 59.90"
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

// Cria o primeiro administrador, se ainda não existir (seção "AdminInicial" do appsettings)
await AdminInicial.CriarAsync(app.Services);

app.Run();
