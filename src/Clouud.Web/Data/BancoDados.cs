using Clouud.Web.Models;
using Microsoft.EntityFrameworkCore;


namespace Clouud.Web.Data
{
    public class BancoDados : DbContext
    {
        //Mapeamento das tabelas do banco de dados
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<CarrinhoItem> CarrinhoItens { get; set; }
        public DbSet<Plataforma> Plataformas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Chave> Chaves { get; set; }



        // A conexão é configurada no Program.cs (AddDbContext) com a connection string "LojaJogos"
        public BancoDados(DbContextOptions<BancoDados> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //desabilita a exclusão em cascata
            foreach (var relacionamento in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relacionamento.DeleteBehavior = DeleteBehavior.Restrict;
            }
            // Jogo x Categoria (N:N) pela tabela JogoCategorias; apagar um lado apaga só o vínculo
            modelBuilder.Entity<Jogo>()
                .HasMany(j => j.Categorias)
                .WithMany(c => c.Jogos)
                .UsingEntity<Dictionary<string, object>>(
                    "JogoCategorias",
                    r => r.HasOne<Categoria>().WithMany().HasForeignKey("CategoriaId").OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<Jogo>().WithMany().HasForeignKey("JogoId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("JogoId", "CategoriaId"));

            // Empresa removida: o jogo fica sem desenvolvedora/publicadora
            modelBuilder.Entity<Jogo>().HasOne(j => j.Desenvolvedora).WithMany().OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Jogo>().HasOne(j => j.Publicadora).WithMany().OnDelete(DeleteBehavior.SetNull);

            // Preço promocional sempre menor que o preço normal
            modelBuilder.Entity<Produto>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Produtos_Preco", "\"Preco\" >= 0");
                t.HasCheckConstraint("CK_Produtos_PrecoPromocional",
                    "\"PrecoPromocional\" IS NULL OR (\"PrecoPromocional\" >= 0 AND \"PrecoPromocional\" < \"Preco\")");
            });

            // Status da chave gravado como texto, para o banco ficar legível
            modelBuilder.Entity<Chave>(chave =>
            {
                chave.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
                chave.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Chaves_Status",
                        "\"Status\" IN ('Disponivel', 'Reservada', 'Vendida', 'Inativa')");
                    // Reservada/vendida sempre tem pedido; disponível nunca tem
                    t.HasCheckConstraint("CK_Chaves_Pedido",
                        "(\"Status\" IN ('Reservada', 'Vendida') AND \"PedidoItemId\" IS NOT NULL) OR (\"Status\" = 'Disponivel' AND \"PedidoItemId\" IS NULL) OR \"Status\" = 'Inativa'");
                });
            });

            modelBuilder.Entity<Pedido>(pedido =>
            {
                pedido.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
                pedido.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Pedidos_Status",
                        "\"Status\" IN ('AguardandoPagamento', 'Pago', 'Cancelado', 'Reembolsado')");
                    t.HasCheckConstraint("CK_Pedidos_Total", "\"Total\" >= 0");
                });
                pedido.HasIndex(p => new { p.Status, p.PagarAte });
            });

            modelBuilder.Entity<PedidoItem>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_PedidoItens_Quantidade", "\"Quantidade\" > 0");
                t.HasCheckConstraint("CK_PedidoItens_Valores", "\"PrecoUnitario\" >= 0 AND \"Subtotal\" >= 0");
            });

            modelBuilder.Entity<Pagamento>(pagamento =>
            {
                pagamento.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);
                pagamento.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
                pagamento.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Pagamentos_Metodo", "\"Metodo\" IN ('Pix', 'Cartao', 'Boleto')");
                    t.HasCheckConstraint("CK_Pagamentos_Status", "\"Status\" IN ('Pendente', 'Aprovado', 'Recusado')");
                });
            });

            // Carrinho: apagar o usuário ou o produto limpa os carrinhos
            modelBuilder.Entity<CarrinhoItem>(item =>
            {
                item.HasOne(i => i.Usuario).WithMany().OnDelete(DeleteBehavior.Cascade);
                item.HasOne(i => i.Produto).WithMany().OnDelete(DeleteBehavior.Cascade);
                item.ToTable(t => t.HasCheckConstraint("CK_CarrinhoItens_Quantidade",
                    $"\"Quantidade\" BETWEEN 1 AND {CarrinhoItem.QuantidadeMaxima}"));
            });

            base.OnModelCreating(modelBuilder);
            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancoDados).Assembly);
        }

    }
}
