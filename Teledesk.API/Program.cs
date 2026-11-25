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

            // Detecta automaticamente SQL Server (LocalDB) ou MySQL conforme a ConnectionString
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada.");

            bool usarSqlServer = connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase);

            builder.Services.AddDbContext<TeledeskDbContext>(options =>
            {
                if (usarSqlServer)
                {
                    options.UseSqlServer(connectionString);
                }
                else
                {
                    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
                }
            });

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

            // Cria/atualiza banco e executa seed inicial
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<TeledeskDbContext>();
                    if (usarSqlServer)
                    {
                        await db.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        await db.Database.MigrateAsync();
                    }

                    await DbInitializer.SeedAsync(db);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Não foi possível inicializar o banco automaticamente. Verifique a ConnectionString.");
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
