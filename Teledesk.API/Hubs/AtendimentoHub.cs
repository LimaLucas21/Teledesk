using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;
using Telegram.Bot;

namespace Teledesk.API.Hubs
{
    public class AtendimentoHub : Hub
    {
        private readonly TeledeskDbContext _context;
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<AtendimentoHub> _logger;

        public AtendimentoHub(
            TeledeskDbContext context,
            ITelegramBotClient botClient,
            ILogger<AtendimentoHub> logger)
        {
            _context = context;
            _botClient = botClient;
            _logger = logger;
        }

        /// <summary>
        /// O atendente assume um atendimento que está na fila de espera.
        /// </summary>
        public async Task<bool> AssumirAtendimento(int atendimentoId, int atendenteId)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            if (atendimento == null)
                return false;

            var atendente = await _context.Atendentes.FindAsync(atendenteId);

            atendimento.AtendenteId = atendenteId;
            atendimento.Status = StatusAtendimento.EmAtendimento;
            await _context.SaveChangesAsync();

            var dto = new AtendimentoDto
            {
                Id = atendimento.Id,
                ContatoId = atendimento.ContatoId,
                NomeCliente = atendimento.Contato.Nome,
                UsernameCliente = atendimento.Contato.Username,
                TelegramChatId = atendimento.Contato.TelegramChatId,
                Status = atendimento.Status,
                AtendenteId = atendenteId,
                AtendenteNome = atendente?.Nome,
                DataInicio = atendimento.DataInicio
            };

            // Notifica todos os atendentes conectados sobre a mudança de status
            await Clients.All.SendAsync("AtendimentoAssumido", dto);

            // Notifica o cliente no Telegram
            var nomeAtendente = atendente?.Nome ?? "um operador";
            await _botClient.SendMessage(
                atendimento.Contato.TelegramChatId,
                $"Olá! Sou {nomeAtendente} e darei continuidade ao seu atendimento. Como posso te ajudar?");

            _logger.LogInformation("Atendimento #{Id} assumido pelo Atendente #{AtendenteId}", atendimentoId, atendenteId);
            return true;
        }

        /// <summary>
        /// O atendente envia uma mensagem de texto para o cliente no Telegram.
        /// </summary>
        public async Task<bool> EnviarMensagemParaCliente(int atendimentoId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            if (atendimento == null || atendimento.Status != StatusAtendimento.EmAtendimento)
                return false;

            // 1. Enviar mensagem para o cliente no Telegram
            await _botClient.SendMessage(atendimento.Contato.TelegramChatId, texto);

            // 2. Persistir no banco de dados como mensagem do Atendente
            var mensagem = new Mensagem
            {
                AtendimentoId = atendimentoId,
                Texto = texto,
                Origem = OrigemMensagem.Atendente,
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            var mensagemDto = new MensagemDto
            {
                Id = mensagem.Id,
                AtendimentoId = mensagem.AtendimentoId,
                Texto = mensagem.Texto,
                Origem = mensagem.Origem,
                DataEnvio = mensagem.DataEnvio
            };

            // Notifica os painéis conectados para refletir a mensagem no chat aberto
            await Clients.All.SendAsync("NovaMensagemRecebida", mensagemDto);
            return true;
        }

        /// <summary>
        /// O atendente encerra o atendimento ativo.
        /// </summary>
        public async Task<bool> FinalizarAtendimento(int atendimentoId)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            if (atendimento == null)
                return false;

            atendimento.Status = StatusAtendimento.Finalizado;
            atendimento.DataFim = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notifica os painéis conectados
            await Clients.All.SendAsync("AtendimentoFinalizado", atendimentoId);

            // Notifica o cliente no Telegram
            await _botClient.SendMessage(
                atendimento.Contato.TelegramChatId,
                "Seu atendimento foi finalizado. Agradecemos pelo contato! Se precisar de mais alguma informação, basta enviar uma nova mensagem.");

            _logger.LogInformation("Atendimento #{Id} finalizado com sucesso.", atendimentoId);
            return true;
        }

        /// <summary>
        /// Retorna a lista atual de atendimentos na fila e em atendimento.
        /// </summary>
        public async Task<List<AtendimentoDto>> ObterFilaAtendimento()
        {
            return await _context.Atendimentos
                .Include(a => a.Contato)
                .Include(a => a.Atendente)
                .Where(a => a.Status == StatusAtendimento.AguardandoAtendente || a.Status == StatusAtendimento.EmAtendimento)
                .OrderBy(a => a.DataInicio)
                .Select(a => new AtendimentoDto
                {
                    Id = a.Id,
                    ContatoId = a.ContatoId,
                    NomeCliente = a.Contato.Nome,
                    UsernameCliente = a.Contato.Username,
                    TelegramChatId = a.Contato.TelegramChatId,
                    Status = a.Status,
                    AtendenteId = a.AtendenteId,
                    AtendenteNome = a.Atendente != null ? a.Atendente.Nome : null,
                    DataInicio = a.DataInicio,
                    UltimaMensagem = a.Mensagens.OrderByDescending(m => m.DataEnvio).Select(m => m.Texto).FirstOrDefault()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retorna o histórico de mensagens de um atendimento específico.
        /// </summary>
        public async Task<List<MensagemDto>> ObterHistoricoMensagens(int atendimentoId)
        {
            return await _context.Mensagens
                .Where(m => m.AtendimentoId == atendimentoId)
                .OrderBy(m => m.DataEnvio)
                .Select(m => new MensagemDto
                {
                    Id = m.Id,
                    AtendimentoId = m.AtendimentoId,
                    Texto = m.Texto,
                    Origem = m.Origem,
                    DataEnvio = m.DataEnvio
                })
                .ToListAsync();
        }
    }
}
