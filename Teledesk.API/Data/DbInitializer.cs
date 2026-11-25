using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;

namespace Teledesk.API.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Garante a criação dos setores iniciais, sessão/número padrão do WAHA e usuário administrador.
        /// </summary>
        public static async Task SeedAsync(TeledeskDbContext context)
        {
            if (!await context.Setores.AnyAsync())
            {
                var setores = new List<Setor>
                {
                    new Setor { Nome = "Suporte Técnico", Descricao = "Dúvidas técnicas, erros e chamados", OrdemUra = 1, Ativo = true },
                    new Setor { Nome = "Financeiro", Descricao = "Boletos, pagamentos e notas fiscais", OrdemUra = 2, Ativo = true },
                    new Setor { Nome = "Comercial", Descricao = "Planos, orçamentos e vendas", OrdemUra = 3, Ativo = true }
                };

                context.Setores.AddRange(setores);
                await context.SaveChangesAsync();
            }

            var todosSetores = await context.Setores.OrderBy(s => s.OrdemUra).ToListAsync();

            var numeroLegado = await context.NumerosWhatsApp
                .FirstOrDefaultAsync(n => n.PhoneNumberId == "100000000000000");

            if (numeroLegado != null && !await context.NumerosWhatsApp.AnyAsync(n => n.PhoneNumberId == "default"))
            {
                numeroLegado.PhoneNumberId = "default";
                numeroLegado.NomeIdentificacao = "Atendimento Principal (Sessão default WAHA)";
                await context.SaveChangesAsync();
            }

            if (!await context.NumerosWhatsApp.AnyAsync())
            {
                var numeroPrincipal = new NumeroWhatsApp
                {
                    NomeIdentificacao = "Atendimento Principal (Sessão default WAHA)",
                    NumeroExibicao = "+55 11 90000-0000",
                    PhoneNumberId = "default",
                    Ativo = true,
                    Setores = todosSetores
                };

                context.NumerosWhatsApp.Add(numeroPrincipal);
                await context.SaveChangesAsync();
            }

            if (!await context.Atendentes.AnyAsync(a => a.Email == "admin@teledesk.com"))
            {
                var hasher = new PasswordHasher<Atendente>();
                var admin = new Atendente
                {
                    Nome = "Administrador",
                    Email = "admin@teledesk.com",
                    Perfil = PerfilAtendente.Admin,
                    Status = StatusAtendente.Disponivel,
                    Ativo = true,
                    Setores = todosSetores
                };
                admin.SenhaHash = hasher.HashPassword(admin, "admin123");

                context.Atendentes.Add(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}
