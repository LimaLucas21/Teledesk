using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Teledesk.API.Data;

namespace Teledesk.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuração do Entity Framework Core com SQL Server
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TeledeskDbContext>(options =>
                options.UseSqlServer(connectionString));

            var token = builder.Configuration["TelegramSettings:Token"];
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(token!));
                
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
