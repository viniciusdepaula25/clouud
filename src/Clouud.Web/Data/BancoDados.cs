using Clouud.Web.Infraestrutura;
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
        public DbSet<ListaDesejo> ListaDesejos { get; set; }
        public DbSet<Cupom> Cupons { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<JogoImagem> JogoImagens { get; set; }
        public DbSet<Plataforma> Plataformas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Chave> Chaves { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<RedefinicaoSenha> RedefinicoesSenha { get; set; }



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
            // Jogo x Categoria (N:N) pela tabela jogo_categorias; apagar um lado apaga só o vínculo
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
                t.HasCheckConstraint("ck_produtos_preco", "preco >= 0");
                t.HasCheckConstraint("ck_produtos_preco_promocional",
                    "preco_promocional IS NULL OR (preco_promocional >= 0 AND preco_promocional < preco)");
            });

            // Status da chave gravado como texto, para o banco ficar legível
            modelBuilder.Entity<Chave>(chave =>
            {
                chave.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
                // Código cifrado com AES-GCM: o banco nunca vê o código aberto
                chave.Property(c => c.Codigo)
                    .HasColumnName("codigo_cifrado")
                    .HasColumnType("text")
                    .HasConversion(
                        codigo => CriptografiaChaves.Atual.Cifrar(codigo),
                        cifrado => CriptografiaChaves.Atual.Decifrar(cifrado));
                chave.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_chaves_status",
                        "status IN ('Disponivel', 'Reservada', 'Vendida', 'Inativa')");
                    // Reservada/vendida sempre tem pedido; disponível nunca tem
                    t.HasCheckConstraint("ck_chaves_pedido",
                        "(status IN ('Reservada', 'Vendida') AND pedido_item_id IS NOT NULL) OR (status = 'Disponivel' AND pedido_item_id IS NULL) OR status = 'Inativa'");
                });
            });

            modelBuilder.Entity<Pedido>(pedido =>
            {
                pedido.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
                pedido.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_pedidos_status",
                        "status IN ('AguardandoPagamento', 'Pago', 'Cancelado', 'Reembolsado')");
                    t.HasCheckConstraint("ck_pedidos_total", "total >= 0");
                    // total sempre bate com subtotal - desconto, e o desconto não passa do subtotal
                    t.HasCheckConstraint("ck_pedidos_desconto", "desconto >= 0 AND desconto <= subtotal AND total = subtotal - desconto");
                });
                pedido.HasIndex(p => new { p.Status, p.PagarAte });
            });

            modelBuilder.Entity<PedidoItem>().ToTable(t =>
            {
                t.HasCheckConstraint("ck_pedido_itens_quantidade", "quantidade > 0");
                t.HasCheckConstraint("ck_pedido_itens_valores", "preco_unitario >= 0 AND subtotal >= 0");
            });

            modelBuilder.Entity<Pagamento>(pagamento =>
            {
                pagamento.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);
                pagamento.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
                pagamento.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_pagamentos_metodo", "metodo IN ('Pix', 'Cartao', 'Boleto')");
                    t.HasCheckConstraint("ck_pagamentos_status", "status IN ('Pendente', 'Aprovado', 'Recusado')");
                });
            });

            // Carrinho: apagar o usuário ou o produto limpa os carrinhos
            modelBuilder.Entity<CarrinhoItem>(item =>
            {
                item.HasOne(i => i.Usuario).WithMany().OnDelete(DeleteBehavior.Cascade);
                item.HasOne(i => i.Produto).WithMany().OnDelete(DeleteBehavior.Cascade);
                item.ToTable(t => t.HasCheckConstraint("ck_carrinho_itens_quantidade",
                    $"quantidade BETWEEN 1 AND {CarrinhoItem.QuantidadeMaxima}"));
            });

            modelBuilder.Entity<Cupom>(cupom =>
            {
                cupom.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
                cupom.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_cupons_tipo", "tipo IN ('Percentual', 'ValorFixo')");
                    t.HasCheckConstraint("ck_cupons_valor", "valor > 0 AND (tipo <> 'Percentual' OR valor <= 100)");
                    t.HasCheckConstraint("ck_cupons_codigo", "codigo = upper(codigo)");
                });
            });

            // Avaliações: nota de 1 a 5; apagar o usuário ou o jogo apaga as avaliações dele
            modelBuilder.Entity<Avaliacao>(avaliacao =>
            {
                avaliacao.HasOne(a => a.Usuario).WithMany().OnDelete(DeleteBehavior.Cascade);
                avaliacao.HasOne(a => a.Jogo).WithMany(j => j.Avaliacoes).OnDelete(DeleteBehavior.Cascade);
                avaliacao.ToTable(t => t.HasCheckConstraint("ck_avaliacoes_nota", "nota BETWEEN 1 AND 5"));
            });

            // Galeria: apagar o jogo apaga as imagens dele (os arquivos são apagados pelo GaleriaService)
            modelBuilder.Entity<JogoImagem>().HasOne(i => i.Jogo).WithMany(j => j.Imagens).OnDelete(DeleteBehavior.Cascade);

            // Lista de desejos: apagar o usuário ou o jogo remove o jogo das listas
            modelBuilder.Entity<ListaDesejo>(desejo =>
            {
                desejo.HasOne(d => d.Usuario).WithMany().OnDelete(DeleteBehavior.Cascade);
                desejo.HasOne(d => d.Jogo).WithMany().OnDelete(DeleteBehavior.Cascade);
            });

            // Fila de e-mails: o tipo fica como texto; apagar o usuário mantém o registro do envio, sem o vínculo
            modelBuilder.Entity<Email>(email =>
            {
                email.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
                email.HasOne(e => e.Usuario).WithMany().OnDelete(DeleteBehavior.SetNull);
            });

            // Pedidos de nova senha somem junto com a conta
            modelBuilder.Entity<RedefinicaoSenha>().HasOne(r => r.Usuario).WithMany().OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);

            // Por último: nomes de tabelas, colunas, chaves e índices em minúsculo snake_case.
            // Os check constraints acima já são escritos com esses nomes.
            NomesSnakeCase.Aplicar(modelBuilder);
        }

    }
}
