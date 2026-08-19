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
        public DbSet<Atendimento> Atendimentos => Set<Atendimento>();
        public DbSet<Mensagem> Mensagens => Set<Mensagem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeamento da Entidade Contato
            modelBuilder.Entity<Contato>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.TelegramChatId).IsUnique();
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
                entity.Property(a => a.Senha).IsRequired().HasMaxLength(150);
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
            });

            // Mapeamento da Entidade Mensagem
            modelBuilder.Entity<Mensagem>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Texto).IsRequired();

                // Relacionamento com Atendimento (1:N)
                entity.HasOne(m => m.Atendimento)
                      .WithMany(a => a.Mensagens)
                      .HasForeignKey(m => m.AtendimentoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
