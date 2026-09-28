using Clouud.Web.Models;
using Clouud.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Clouud.Web.Data
{
    /// <summary>
    /// Cria o primeiro administrador ao iniciar a aplicação, se ainda não existir nenhum.
    /// O cadastro pelo site só cria clientes. O e-mail vem de "AdminInicial:Email"; a senha, de
    /// "AdminInicial:Senha", que nunca fica no appsettings (variável AdminInicial__Senha ou user-secrets).
    /// </summary>
    public static class AdminInicial
    {
        public static async Task CriarAsync(IServiceProvider services)
        {
            using var escopo = services.CreateScope();
            var bancoDados = escopo.ServiceProvider.GetRequiredService<BancoDados>();
            var senhas = escopo.ServiceProvider.GetRequiredService<SenhaService>();
            var config = escopo.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminInicial));

            var email = config["AdminInicial:Email"]?.Trim().ToLowerInvariant();
            var senha = config["AdminInicial:Senha"];
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            try
            {
                if ((await bancoDados.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogWarning("O banco tem migrations pendentes. Rode 'dotnet ef database update' para criar o admin inicial.");
                    return;
                }
                if (await bancoDados.Usuarios.AnyAsync(u => u.Perfil == PerfilUsuario.Admin))
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(senha) || senha.Length < SenhaService.TamanhoMinimo)
                {
                    logger.LogWarning(
                        "Ainda não há administrador. Para criar o primeiro, defina uma senha de pelo menos {Minimo} caracteres " +
                        "em AdminInicial__Senha (variável de ambiente) ou, em desenvolvimento, com: " +
                        "dotnet user-secrets --project src/Clouud.Web set \"AdminInicial:Senha\" \"uma-senha-forte\"",
                        SenhaService.TamanhoMinimo);
                    return;
                }

                if (await bancoDados.Usuarios.AnyAsync(u => u.Email == email))
                {
                    logger.LogWarning("Já existe um usuário com o e-mail {Email}; o admin inicial não foi criado.", email);
                    return;
                }

                var admin = new Usuario
                {
                    Name = "Administrador",
                    Email = email,
                    Perfil = PerfilUsuario.Admin
                };
                admin.Senha = senhas.GerarHash(admin, senha);

                bancoDados.Usuarios.Add(admin);
                await bancoDados.SaveChangesAsync();
                logger.LogWarning("Admin inicial criado: {Email}.", email);
            }
            catch (Exception erro)
            {
                logger.LogWarning(erro, "Não foi possível verificar/criar o admin inicial. O banco está acessível?");
            }
        }
    }
}
