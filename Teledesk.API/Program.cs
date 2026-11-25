using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.Hubs;
using Teledesk.API.Security;
using Teledesk.API.Services;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuração do Entity Framework Core com MySQL (Pomelo)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TeledeskDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddHttpClient("WhatsAppCloud");

            // Autenticação JWT Bearer nativa e Autorização
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            builder.Services.AddAuthentication("Bearer")
                .AddScheme<AuthenticationSchemeOptions, JwtBearerAuthHandler>("Bearer", null);
            builder.Services.AddAuthorization();

            // Configuração do SignalR para comunicação em tempo real por setor
            builder.Services.AddSignalR();

            // Configuração de CORS para conexões locais/desktop
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .SetIsOriginAllowed(_ => true)
                          .AllowCredentials();
                });
            });

            builder.Services.AddScoped<IWhatsAppService, WhatsAppCloudService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();

            builder.WebHost.UseUrls("http://localhost:5000");

            var app = builder.Build();

            // Aplica migrations / seed inicial se o MySQL estiver acessível
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<TeledeskDbContext>();
                    await db.Database.MigrateAsync();
                    await DbInitializer.SeedAsync(db);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Não foi possível aplicar migrations/seed automaticamente na inicialização. Verifique se o MySQL está rodando.");
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<AtendimentoHub>("/hubs/atendimento");

            await app.RunAsync();
        }
    }
}
