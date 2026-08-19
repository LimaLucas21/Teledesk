using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Services
{
    public class TelegramPollingService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramPollingService> _logger;

        public TelegramPollingService(
            ITelegramBotClient botClient,
            IServiceProvider serviceProvider,
            ILogger<TelegramPollingService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando serviço de Polling do Telegram...");

            try
            {
                // Remove qualquer webhook registrado anteriormente para permitir o polling local
                await _botClient.DropPendingUpdates(stoppingToken);

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken
                );

                _logger.LogInformation("🤖 Bot do Telegram escutando mensagens ativamente!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar o Polling do Telegram.");
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
                await conversationService.ProcessUpdateAsync(update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem recebida pelo Polling.");
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Erro de polling na API do Telegram.");
            return Task.CompletedTask;
        }
    }
}
