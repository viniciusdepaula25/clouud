using Microsoft.EntityFrameworkCore;


namespace ProjetoFinal_LojadeJogos.Data
{
    public class BancoDados : DbContext
    {
        //Mapeamento das tabelas do banco de dados
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoJ> PedidoJs { get; set; }
        public DbSet<Cliente> Clientes { get; set; }    



        public BancoDados()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //configuração do banco de dados
            // lê a connection string "LojaJogos" do appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            optionsBuilder.UseNpgsql(config.GetConnectionString("LojaJogos"));
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //desabilita a exclusão em cascata
            foreach (var relacionamento in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relacionamento.DeleteBehavior = DeleteBehavior.Restrict;
            }
            base.OnModelCreating(modelBuilder);
            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancoDados).Assembly);
        }

    }
}
