using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace Teledesk.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<BotController> _logger;

        public BotController(ITelegramBotClient botClient, ILogger<BotController> logger)
        {
            _botClient = botClient;
            _logger = logger;

        }

        [HttpPost]
        public IActionResult Post([FromBody] Update update)
        {
            string usuario = "";
            string texto = "";

            if (update.Message?.Text != null)
            {
                usuario = update.Message.From?.FirstName ?? "Usuário";
                texto = update.Message.Text;
                _logger.LogInformation("Mensagem recebida de {usuario}: {texto}", usuario, texto);
    
        }
            return Ok();
        }

    }
}