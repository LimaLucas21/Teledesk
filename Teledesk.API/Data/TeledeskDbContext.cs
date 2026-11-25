using Microsoft.EntityFrameworkCore;
using Teledesk.API.Models;

namespace Teledesk.API.Data
{
    public class TeledeskDbContext : DbContext
    {
        public TeledeskDbContext(DbContextOptions<TeledeskDbContext> options)
            : base(options)
        {
        }

        public DbSet<Contato> Contatos => Set<Contato>();
        public DbSet<Atendente> Atendentes => Set<Atendente>();
        public DbSet<Setor> Setores => Set<Setor>();
        public DbSet<NumeroWhatsApp> NumerosWhatsApp => Set<NumeroWhatsApp>();
        public DbSet<Atendimento> Atendimentos => Set<Atendimento>();
        public DbSet<Mensagem> Mensagens => Set<Mensagem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeamento da Entidade Contato
            modelBuilder.Entity<Contato>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.WhatsAppId).IsUnique();
                entity.Property(c => c.WhatsAppId).IsRequired().HasMaxLength(30);
                entity.Property(c => c.Nome).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Username).HasMaxLength(100);
            });

            // Mapeamento da Entidade Atendente
            modelBuilder.Entity<Atendente>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.Email).IsUnique();
                entity.Property(a => a.Nome).IsRequired().HasMaxLength(150);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(150);
                entity.Property(a => a.SenhaHash).IsRequired().HasMaxLength(255);

                entity.HasMany(a => a.Setores)
                      .WithMany(s => s.Atendentes)
                      .UsingEntity(j => j.ToTable("AtendenteSetores"));
            });

            // Mapeamento da Entidade Setor
            modelBuilder.Entity<Setor>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.Nome).IsUnique();
                entity.Property(s => s.Nome).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Descricao).HasMaxLength(250);
            });

            // Mapeamento da Entidade NumeroWhatsApp
            modelBuilder.Entity<NumeroWhatsApp>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.HasIndex(n => n.PhoneNumberId).IsUnique();
                entity.Property(n => n.NomeIdentificacao).IsRequired().HasMaxLength(120);
                entity.Property(n => n.NumeroExibicao).IsRequired().HasMaxLength(30);
                entity.Property(n => n.PhoneNumberId).IsRequired().HasMaxLength(100);
                entity.Property(n => n.AccessToken).HasMaxLength(500);

                entity.HasMany(n => n.Setores)
                      .WithMany(s => s.NumerosWhatsApp)
                      .UsingEntity(j => j.ToTable("NumeroWhatsAppSetores"));
            });

            // Mapeamento da Entidade Atendimento
            modelBuilder.Entity<Atendimento>(entity =>
            {
                entity.HasKey(a => a.Id);

                // Relacionamento com Contato (1:N)
                entity.HasOne(a => a.Contato)
                      .WithMany(c => c.Atendimentos)
                      .HasForeignKey(a => a.ContatoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relacionamento com Atendente (1:N Opcional)
                entity.HasOne(a => a.Atendente)
                      .WithMany(at => at.Atendimentos)
                      .HasForeignKey(a => a.AtendenteId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Relacionamento com Setor (1:N Opcional enquanto na URA)
                entity.HasOne(a => a.Setor)
                      .WithMany(s => s.Atendimentos)
                      .HasForeignKey(a => a.SetorId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Relacionamento com NumeroWhatsApp (1:N Opcional)
                entity.HasOne(a => a.NumeroWhatsApp)
                      .WithMany(n => n.Atendimentos)
                      .HasForeignKey(a => a.NumeroWhatsAppId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Mapeamento da Entidade Mensagem
            modelBuilder.Entity<Mensagem>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Texto).IsRequired();
                entity.Property(m => m.WhatsAppMessageId).HasMaxLength(120);
                entity.HasIndex(m => m.WhatsAppMessageId).IsUnique();

                // Relacionamento com Atendimento (1:N)
                entity.HasOne(m => m.Atendimento)
                      .WithMany(a => a.Mensagens)
                      .HasForeignKey(m => m.AtendimentoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
