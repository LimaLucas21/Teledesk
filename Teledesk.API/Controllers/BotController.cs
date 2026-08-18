using Microsoft.AspNetCore.Mvc;
using Teledesk.API.Services.Interfaces;
using Telegram.Bot.Types;

namespace Teledesk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<BotController> _logger;

        public BotController(IConversationService conversationService, ILogger<BotController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            try
            {
                await _conversationService.ProcessUpdateAsync(update);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar update do Telegram.");
                // Retornar 200 OK para o Telegram não tentar reenviar a mesma mensagem repetidamente em caso de falha transitória
                return Ok();
            }
        }
    }
}