using Telegram.Bot.Types;

namespace Teledesk.API.Services.Interfaces
{
    public interface IConversationService
    {
        Task ProcessUpdateAsync(Update update);

    }
}
