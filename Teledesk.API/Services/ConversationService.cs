using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Hubs;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Services
{
    public class ConversationService : IConversationService
    {
        // Locks por remetente e cache em memória de IDs de mensagens para impedir duplicação em webhooks simultâneos
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> LocksPorContato = new();
        private static readonly ConcurrentDictionary<string, DateTime> MensagensProcessadasCache = new();
        private const int MaxIdadeMensagemSegundos = 60;

        private readonly TeledeskDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IHubContext<AtendimentoHub> _hubContext;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            TeledeskDbContext context,
            IWhatsAppService whatsAppService,
            IHubContext<AtendimentoHub> hubContext,
            ILogger<ConversationService> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task ProcessWebhookPayloadAsync(WhatsAppWebhookPayload payload)
        {
            if (payload?.Entry == null)
                return;

            foreach (var entry in payload.Entry)
            {
                foreach (var change in entry.Changes)
                {
                    var value = change.Value;
                    if (value?.Messages == null || value.Messages.Count == 0)
                        continue;

                    var phoneNumberId = value.Metadata?.PhoneNumberId ?? "default";
                    var displayPhone = value.Metadata?.DisplayPhoneNumber;

                    foreach (var msg in value.Messages)
                    {
                        if (!string.Equals(msg.Type, "text", StringComparison.OrdinalIgnoreCase) ||
                            string.IsNullOrWhiteSpace(msg.Text?.Body))
                        {
                            continue;
                        }

                        var waId = msg.From ?? value.Contacts.FirstOrDefault()?.WaId ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(waId))
                            continue;

                        var nomeContato = value.Contacts
                            .FirstOrDefault(c => c.WaId == waId)?.Profile?.Name
                            ?? value.Contacts.FirstOrDefault()?.Profile?.Name
                            ?? $"Cliente {waId}";

                        await ProcessIncomingWhatsAppMessageAsync(
                            phoneNumberId,
                            waId,
                            nomeContato,
                            msg.Text.Body,
                            msg.Id,
                            displayPhone);
                    }
                }
            }
        }

        public async Task ProcessWahaWebhookAsync(WahaWebhookPayload payload)
        {
            if (payload?.Payload == null)
                return;

            // Processa APENAS o evento "message" (evita duplicidade concorrente com "message.any")
            if (!string.Equals(payload.Event, "message", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (payload.Payload.FromMe || string.IsNullOrWhiteSpace(payload.Payload.Body))
                return;

            // Ignora mensagens antigas vindas de sincronização de histórico ao conectar QR Code / reiniciar WAHA
            if (payload.Payload.Timestamp.HasValue && payload.Payload.Timestamp.Value > 0)
            {
                var msgEpochSeconds = payload.Payload.Timestamp.Value > 10_000_000_000L
                    ? payload.Payload.Timestamp.Value / 1000
                    : payload.Payload.Timestamp.Value;

                var idadeSegundos = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - msgEpochSeconds;
                if (idadeSegundos > MaxIdadeMensagemSegundos)
                {
                    _logger.LogInformation(
                        "⏭️ Ignorando mensagem antiga do histórico do WhatsApp (idade: {Idade}s, id: {Id})",
                        idadeSegundos, payload.Payload.Id);
                    return;
                }
            }

            var rawFrom = payload.Payload.From ?? payload.Payload.ChatId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawFrom))
                return;

            // Ignora grupos, listas de transmissão e canais
            if (rawFrom.EndsWith("@g.us", StringComparison.OrdinalIgnoreCase) ||
                rawFrom.EndsWith("@broadcast", StringComparison.OrdinalIgnoreCase) ||
                rawFrom.EndsWith("@newsletter", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var session = string.IsNullOrWhiteSpace(payload.Session) ? "default" : payload.Session.Trim();
            var waId = await ResolverIdentificadorRemetenteWahaAsync(
                session,
                rawFrom,
                payload.Payload.Data?.Key,
                payload.Payload.Data?.Info?.SenderAlt);

            var nomeExtraido = payload.Payload.Data?.Info?.PushName
                ?? payload.Payload.Data?.PushName
                ?? payload.Payload.Data?.NotifyName
                ?? payload.Payload.NotifyName;

            var nomeCliente = !string.IsNullOrWhiteSpace(nomeExtraido)
                ? nomeExtraido.Trim()
                : $"Cliente {ExibirIdentificadorCurto(waId)}";

            var displayPhone = NormalizarIdentificadorWhatsApp(payload.Me?.Id);

            await ProcessIncomingWhatsAppMessageAsync(
                session,
                waId,
                nomeCliente,
                payload.Payload.Body,
                payload.Payload.Id,
                displayPhone);
        }

        public async Task<ResultadoProcessamentoWhatsApp> ProcessIncomingWhatsAppMessageAsync(
            string phoneNumberId,
            string waId,
            string nomeCliente,
            string texto,
            string? whatsAppMessageId = null,
            string? displayPhoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(waId) || string.IsNullOrWhiteSpace(texto))
            {
                return new ResultadoProcessamentoWhatsApp
                {
                    Sucesso = false,
                    Observacao = "WhatsAppId e texto são obrigatórios."
                };
            }

            texto = texto.Trim();
            waId = NormalizarIdentificadorWhatsApp(waId);
            phoneNumberId = string.IsNullOrWhiteSpace(phoneNumberId) || phoneNumberId == "100000000000000"
                ? "default"
                : phoneNumberId.Trim();

            // Deduplicação atômica em memória antes mesmo de ir ao banco
            if (!string.IsNullOrWhiteSpace(whatsAppMessageId) &&
                !MensagensProcessadasCache.TryAdd(whatsAppMessageId, DateTime.UtcNow))
            {
                _logger.LogInformation("Mensagem duplicada bloqueada em memória (id: {MessageId})", whatsAppMessageId);
                return new ResultadoProcessamentoWhatsApp
                {
                    Sucesso = true,
                    Observacao = "Mensagem duplicada ignorada."
                };
            }

            var semaforo = LocksPorContato.GetOrAdd(waId, _ => new SemaphoreSlim(1, 1));
            await semaforo.WaitAsync();
            try
            {
                // 1. Idempotência no banco
                if (!string.IsNullOrWhiteSpace(whatsAppMessageId))
                {
                    var jaProcessada = await _context.Mensagens
                        .AnyAsync(m => m.WhatsAppMessageId == whatsAppMessageId);

                    if (jaProcessada)
                    {
                        _logger.LogInformation("Mensagem duplicada ignorada no banco (id: {MessageId})", whatsAppMessageId);
                        return new ResultadoProcessamentoWhatsApp
                        {
                            Sucesso = true,
                            Observacao = "Mensagem já processada anteriormente (idempotência)."
                        };
                    }
                }

                // 2. Localizar o Número/Sessão WhatsApp
                var numeroWhatsApp = await _context.NumerosWhatsApp
                    .Include(n => n.Setores)
                    .FirstOrDefaultAsync(n => n.PhoneNumberId == phoneNumberId && n.Ativo);

                if (numeroWhatsApp == null)
                {
                    var todosSetores = await _context.Setores.Where(s => s.Ativo).OrderBy(s => s.OrdemUra).ToListAsync();
                    numeroWhatsApp = new NumeroWhatsApp
                    {
                        NomeIdentificacao = "Central de Atendimento",
                        NumeroExibicao = displayPhoneNumber ?? phoneNumberId,
                        PhoneNumberId = phoneNumberId,
                        Ativo = true,
                        Setores = todosSetores
                    };
                    _context.NumerosWhatsApp.Add(numeroWhatsApp);
                    await _context.SaveChangesAsync();
                }

                var setoresAtivos = numeroWhatsApp.Setores
                    .Where(s => s.Ativo)
                    .OrderBy(s => s.OrdemUra)
                    .ThenBy(s => s.Id)
                    .ToList();

                if (setoresAtivos.Count == 0)
                {
                    setoresAtivos = await _context.Setores
                        .Where(s => s.Ativo)
                        .OrderBy(s => s.OrdemUra)
                        .ThenBy(s => s.Id)
                        .ToListAsync();
                }

                // 3. Identificar ou cadastrar o Contato
                var waIdSemSufixo = ExibirIdentificadorCurto(waId);
                var contato = await _context.Contatos
                    .FirstOrDefaultAsync(c => c.WhatsAppId == waId || c.WhatsAppId == waIdSemSufixo);

                if (contato == null)
                {
                    contato = new Contato
                    {
                        WhatsAppId = waId,
                        Nome = string.IsNullOrWhiteSpace(nomeCliente) ? $"Cliente {waIdSemSufixo}" : nomeCliente,
                        Username = waIdSemSufixo,
                        DataCriacao = DateTime.UtcNow
                    };
                    _context.Contatos.Add(contato);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    bool alterouContato = false;
                    if (contato.WhatsAppId != waId)
                    {
                        contato.WhatsAppId = waId;
                        alterouContato = true;
                    }
                    if (!string.IsNullOrWhiteSpace(nomeCliente) &&
                        !nomeCliente.StartsWith("Cliente ", StringComparison.OrdinalIgnoreCase) &&
                        contato.Nome != nomeCliente)
                    {
                        contato.Nome = nomeCliente;
                        alterouContato = true;
                    }
                    if (alterouContato)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                // 4. Localizar ou iniciar Atendimento ativo
                var atendimento = await _context.Atendimentos
                    .Include(a => a.Setor)
                    .Include(a => a.NumeroWhatsApp)
                    .FirstOrDefaultAsync(a => a.ContatoId == contato.Id && a.Status != StatusAtendimento.Finalizado);

                bool novoAtendimento = false;
                if (atendimento == null)
                {
                    novoAtendimento = true;
                    if (setoresAtivos.Count == 1)
                    {
                        var setorUnico = setoresAtivos[0];
                        atendimento = new Atendimento
                        {
                            ContatoId = contato.Id,
                            NumeroWhatsAppId = numeroWhatsApp.Id,
                            SetorId = setorUnico.Id,
                            Status = StatusAtendimento.AguardandoAtendente,
                            DataInicio = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        atendimento = new Atendimento
                        {
                            ContatoId = contato.Id,
                            NumeroWhatsAppId = numeroWhatsApp.Id,
                            Status = StatusAtendimento.EmUra,
                            DataInicio = DateTime.UtcNow
                        };
                    }

                    _context.Atendimentos.Add(atendimento);
                    await _context.SaveChangesAsync();
                }

                // 5. Persistir mensagem do cliente
                var mensagem = new Mensagem
                {
                    AtendimentoId = atendimento.Id,
                    WhatsAppMessageId = whatsAppMessageId,
                    Texto = texto,
                    Origem = OrigemMensagem.Cliente,
                    DataEnvio = DateTime.UtcNow
                };
                _context.Mensagens.Add(mensagem);
                await _context.SaveChangesAsync();

                var mensagemClienteDto = new MensagemDto
                {
                    Id = mensagem.Id,
                    AtendimentoId = mensagem.AtendimentoId,
                    Texto = mensagem.Texto,
                    Origem = mensagem.Origem,
                    DataEnvio = mensagem.DataEnvio
                };

                // 6. Roteamento conforme estado do atendimento
                if (novoAtendimento && atendimento.Status == StatusAtendimento.AguardandoAtendente && atendimento.SetorId.HasValue)
                {
                    var setorUnico = setoresAtivos[0];
                    var resposta = $"Olá, {contato.Nome}! Você entrou em contato com o setor *{setorUnico.Nome}*. Aguarde um instante, um atendente já irá te responder.";
                    await EnviarMensagemSistemaAsync(atendimento, numeroWhatsApp, contato.WhatsAppId, resposta);
                    await NotificarConversaNoPainelAsync(atendimento, contato, setorUnico, numeroWhatsApp, texto);

                    return new ResultadoProcessamentoWhatsApp
                    {
                        Sucesso = true,
                        AtendimentoId = atendimento.Id,
                        SetorId = setorUnico.Id,
                        SetorNome = setorUnico.Nome,
                        RespostaAutomatica = resposta,
                        Observacao = $"Roteado diretamente para a fila do setor {setorUnico.Nome}."
                    };
                }

                if (atendimento.Status == StatusAtendimento.EmUra)
                {
                    return await ProcessarUraMultiSetorAsync(
                        atendimento,
                        numeroWhatsApp,
                        setoresAtivos,
                        contato,
                        texto,
                        novoAtendimento,
                        mensagemClienteDto);
                }

                await EnviarParaGrupoSetorAsync(atendimento.SetorId, "NovaMensagemRecebida", mensagemClienteDto);

                return new ResultadoProcessamentoWhatsApp
                {
                    Sucesso = true,
                    AtendimentoId = atendimento.Id,
                    SetorId = atendimento.SetorId,
                    SetorNome = atendimento.Setor?.Nome,
                    Observacao = $"Mensagem encaminhada para a fila/atendimento ({atendimento.Status})."
                };
            }
            finally
            {
                semaforo.Release();
            }
        }

        private async Task<ResultadoProcessamentoWhatsApp> ProcessarUraMultiSetorAsync(
            Atendimento atendimento,
            NumeroWhatsApp numeroWhatsApp,
            List<Setor> setoresAtivos,
            Contato contato,
            string texto,
            bool novoAtendimento,
            MensagemDto mensagemClienteDto)
        {
            if (setoresAtivos.Count > 0)
            {
                Setor? setorEscolhido = null;

                if (int.TryParse(texto, out int opcao) && opcao >= 1 && opcao <= setoresAtivos.Count)
                {
                    setorEscolhido = setoresAtivos[opcao - 1];
                }
                else if (!novoAtendimento)
                {
                    setorEscolhido = setoresAtivos.FirstOrDefault(s =>
                        string.Equals(s.Nome, texto, StringComparison.OrdinalIgnoreCase));
                }

                if (setorEscolhido != null)
                {
                    atendimento.SetorId = setorEscolhido.Id;
                    atendimento.Status = StatusAtendimento.AguardandoAtendente;
                    await _context.SaveChangesAsync();

                    var respostaTransferencia = $"✅ Você escolheu o setor *{setorEscolhido.Nome}*. Aguarde um instante que um de nossos atendentes desse setor irá te atender!";
                    await EnviarMensagemSistemaAsync(atendimento, numeroWhatsApp, contato.WhatsAppId, respostaTransferencia);

                    await _hubContext.Clients.All.SendAsync("NovaConversaNaFila", CriarAtendimentoDto(atendimento, contato, setorEscolhido, numeroWhatsApp, texto));
                    await EnviarParaGrupoSetorAsync(setorEscolhido.Id, "NovaMensagemRecebida", mensagemClienteDto);

                    return new ResultadoProcessamentoWhatsApp
                    {
                        Sucesso = true,
                        AtendimentoId = atendimento.Id,
                        SetorId = setorEscolhido.Id,
                        SetorNome = setorEscolhido.Nome,
                        RespostaAutomatica = respostaTransferencia,
                        Observacao = $"Cliente escolheu o setor {setorEscolhido.Nome} e entrou na fila."
                    };
                }
            }

            // Se não é novo atendimento, verifica há quanto tempo o último menu da URA foi enviado para não repetir em rajadas
            if (!novoAtendimento)
            {
                var ultimoEnvioSistema = await _context.Mensagens
                    .Where(m => m.AtendimentoId == atendimento.Id && m.Origem == OrigemMensagem.Sistema)
                    .OrderByDescending(m => m.DataEnvio)
                    .Select(m => (DateTime?)m.DataEnvio)
                    .FirstOrDefaultAsync();

                if (ultimoEnvioSistema.HasValue)
                {
                    var segundosDesdeUltimoMenu = (DateTime.UtcNow - ultimoEnvioSistema.Value).TotalSeconds;

                    // Se o cliente mandou várias mensagens seguidas em menos de 30s ("Oi", "Tudo bem?"), não reenvia o menu
                    if (segundosDesdeUltimoMenu < 30 && !int.TryParse(texto, out _))
                    {
                        await _hubContext.Clients.All.SendAsync("NovaConversaNaFila", CriarAtendimentoDto(atendimento, contato, null, numeroWhatsApp, texto));
                        await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", mensagemClienteDto);

                        return new ResultadoProcessamentoWhatsApp
                        {
                            Sucesso = true,
                            AtendimentoId = atendimento.Id,
                            Observacao = "Mensagem complementar registrada sem reenviar menu da URA."
                        };
                    }

                    // Se digitou número inválido ou já passou mais de 30s, envia apenas um lembrete curto
                    var lembrete = $"Por favor, digite apenas o número de *1* a *{setoresAtivos.Count}* correspondente ao setor desejado.";
                    var msgLembreteDto = await EnviarMensagemSistemaAsync(atendimento, numeroWhatsApp, contato.WhatsAppId, lembrete);

                    await _hubContext.Clients.All.SendAsync("NovaConversaNaFila", CriarAtendimentoDto(atendimento, contato, null, numeroWhatsApp, texto));
                    await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", mensagemClienteDto);
                    await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", msgLembreteDto);

                    return new ResultadoProcessamentoWhatsApp
                    {
                        Sucesso = true,
                        AtendimentoId = atendimento.Id,
                        RespostaAutomatica = lembrete,
                        Observacao = "Lembrete curto da URA enviado."
                    };
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Olá, {contato.Nome}! Bem-vindo ao *{numeroWhatsApp.NomeIdentificacao}* 💬");
            sb.AppendLine();
            sb.AppendLine("Escolha o setor com o qual deseja falar:");

            for (int i = 0; i < setoresAtivos.Count; i++)
            {
                var s = setoresAtivos[i];
                var desc = !string.IsNullOrWhiteSpace(s.Descricao) ? $" ({s.Descricao})" : "";
                sb.AppendLine($"*{i + 1}* - {s.Nome}{desc}");
            }

            sb.AppendLine();
            sb.Append("Digite apenas o número da opção desejada:");

            var menu = sb.ToString();
            var mensagemSistemaDto = await EnviarMensagemSistemaAsync(atendimento, numeroWhatsApp, contato.WhatsAppId, menu);

            await _hubContext.Clients.All.SendAsync("NovaConversaNaFila", CriarAtendimentoDto(atendimento, contato, null, numeroWhatsApp, texto));
            await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", mensagemClienteDto);
            await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", mensagemSistemaDto);

            return new ResultadoProcessamentoWhatsApp
            {
                Sucesso = true,
                AtendimentoId = atendimento.Id,
                RespostaAutomatica = menu,
                Observacao = "Atendimento em URA aguardando escolha do setor."
            };
        }

        private async Task<MensagemDto> EnviarMensagemSistemaAsync(
            Atendimento atendimento,
            NumeroWhatsApp numeroWhatsApp,
            string destinatarioWaId,
            string texto)
        {
            await _whatsAppService.EnviarMensagemTextoAsync(
                numeroWhatsApp.PhoneNumberId,
                destinatarioWaId,
                texto,
                numeroWhatsApp.AccessToken);

            var mensagemSistema = new Mensagem
            {
                AtendimentoId = atendimento.Id,
                Texto = texto,
                Origem = OrigemMensagem.Sistema,
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagemSistema);
            await _context.SaveChangesAsync();

            return new MensagemDto
            {
                Id = mensagemSistema.Id,
                AtendimentoId = mensagemSistema.AtendimentoId,
                Texto = mensagemSistema.Texto,
                Origem = mensagemSistema.Origem,
                DataEnvio = mensagemSistema.DataEnvio
            };
        }

        private async Task NotificarConversaNoPainelAsync(
            Atendimento atendimento,
            Contato contato,
            Setor? setor,
            NumeroWhatsApp numeroWhatsApp,
            string ultimaMensagem)
        {
            var atendimentoDto = CriarAtendimentoDto(atendimento, contato, setor, numeroWhatsApp, ultimaMensagem);
            await EnviarParaGrupoSetorAsync(setor?.Id, "NovaConversaNaFila", atendimentoDto);
        }

        private static AtendimentoDto CriarAtendimentoDto(
            Atendimento atendimento,
            Contato contato,
            Setor? setor,
            NumeroWhatsApp numeroWhatsApp,
            string ultimaMensagem)
        {
            return new AtendimentoDto
            {
                Id = atendimento.Id,
                ContatoId = contato.Id,
                NomeCliente = contato.Nome,
                UsernameCliente = contato.Username,
                WhatsAppId = contato.WhatsAppId,
                SetorId = setor?.Id ?? atendimento.SetorId,
                SetorNome = setor?.Nome ?? (atendimento.Status == StatusAtendimento.EmUra ? "Na URA" : "Sem Setor"),
                NumeroWhatsAppId = numeroWhatsApp.Id,
                NumeroExibicao = numeroWhatsApp.NumeroExibicao,
                Status = atendimento.Status,
                DataInicio = atendimento.DataInicio,
                UltimaMensagem = ultimaMensagem
            };
        }

        private async Task EnviarParaGrupoSetorAsync(int? setorId, string evento, object payload)
        {
            if (setorId.HasValue)
            {
                await _hubContext.Clients.Group($"setor-{setorId.Value}").SendAsync(evento, payload);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync(evento, payload);
            }
        }

        private async Task<string> ResolverIdentificadorRemetenteWahaAsync(
            string session,
            string rawFrom,
            WahaMessageKeyDto? key,
            string? gowsSenderAlt = null)
        {
            var candidatos = new[] { gowsSenderAlt, key?.RemoteJidAlt, key?.SenderPn, key?.ParticipantAlt };
            foreach (var candidato in candidatos)
            {
                if (!string.IsNullOrWhiteSpace(candidato) &&
                    !candidato.EndsWith("@lid", StringComparison.OrdinalIgnoreCase))
                {
                    return NormalizarIdentificadorWhatsApp(candidato);
                }
            }

            if (rawFrom.EndsWith("@lid", StringComparison.OrdinalIgnoreCase))
            {
                var telefoneResolvido = await _whatsAppService.ResolverLidParaTelefoneAsync(session, rawFrom);
                if (!string.IsNullOrWhiteSpace(telefoneResolvido))
                {
                    return NormalizarIdentificadorWhatsApp(telefoneResolvido);
                }

                return rawFrom.Trim();
            }

            return NormalizarIdentificadorWhatsApp(rawFrom);
        }

        private static string NormalizarIdentificadorWhatsApp(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var limpo = id.Trim();
            if (limpo.EndsWith("@lid", StringComparison.OrdinalIgnoreCase))
            {
                return limpo;
            }

            var idxArroba = limpo.IndexOf('@');
            if (idxArroba > 0)
                limpo = limpo[..idxArroba];

            // Remove eventual sufixo de device (:69) caso presente
            var idxDoisPontos = limpo.IndexOf(':');
            if (idxDoisPontos > 0)
                limpo = limpo[..idxDoisPontos];

            return limpo.TrimStart('+');
        }

        private static string ExibirIdentificadorCurto(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var idx = id.IndexOf('@');
            return idx > 0 ? id[..idx] : id;
        }
    }
}
