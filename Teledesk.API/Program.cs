using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Teledesk.API.Data;
using Teledesk.API.Hubs;
using Teledesk.API.Services;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuração do Entity Framework Core com MySQL (Pomelo)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TeledeskDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

            var token = builder.Configuration["TelegramSettings:Token"];

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // Configuração do SignalR para comunicação em tempo real
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

            builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(token!));
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddHostedService<TelegramPollingService>();

            builder.WebHost.UseUrls("http://localhost:5000");
                
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseCors("AllowAll");
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<AtendimentoHub>("/hubs/atendimento");

            app.Run();
        }
    }
}
