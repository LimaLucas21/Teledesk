# Plano de Ação — Evolução do Teledesk

> **Modo de Execução:** A implementação de cada fase seguirá o modo didático/mentoria definido em [`.agents/AGENTS.md`](./.agents/AGENTS.md) (passo a passo incremental, sem entregar o código completo pronto de uma só vez).

---

## 1. Estado Atual Real (Base em `main`)

O repositório já possui a base funcional em **.NET 9**, **ASP.NET Core Web API**, **EF Core + MySQL (Pomelo)**, **SignalR** e **WinForms**:

- **Persistência (MySQL):** `TeledeskDbContext` com entidades `Contato`, `Atendente` (com `Senha` em texto puro e sem perfil/roles), `Atendimento` e `Mensagem` (`InitialCreateMySql` e `AdicionarSenhaAtendente`).
- **Canal Telegram:** `BotController`, `TelegramPollingService` e `ITelegramBotClient` injetado nos serviços.
- **Motor de URA:** `ConversationService` (`IConversationService`) com fluxo fixo (`1 - Horário`, `2 - Falar com Atendente Humano`).
- **Tempo Real:** `AtendimentoHub` em `/hubs/atendimento` fazendo broadcast global (`Clients.All`) sem autenticação ou separação por setor.
- **Login & Desktop:** `AtendentesController` (`POST /api/atendentes/login` com senha em texto puro, auto-cadastro aberto ao tentar e-mail inexistente e sem JWT) consumido pelo projeto `Teledesk.Desktop` (`LoginForm` e `Form1`).

### O que precisa evoluir a partir dessa base:
1. **Autenticação Segura com JWT:** substituir senha em texto puro por hash, remover auto-cadastro aberto, adicionar perfis (`Atendente` / `Admin`) e proteger endpoints + SignalR com JWT Bearer no servidor local.
2. **Migração do Telegram para WhatsApp Cloud API (Oficial da Meta):** remover `Telegram.Bot` / `TelegramPollingService` e integrar Webhook oficial + Graph API.
3. **Estrutura Multi-Setor e Multi-Número:** permitir que um único número atenda vários setores via URA dinâmica **ou** que um número dedicado seja vinculado a um único setor, entregando as filas no SignalR apenas aos atendentes daquele setor.

---

## 2. Arquitetura Alvo

```text
[ Cliente (WhatsApp) ]
        │
        ▼ (Webhook HTTP - WhatsApp Cloud API / Meta)
[ ASP.NET Core Web API (.NET 9 + JWT + SignalR por Setor) ] ◄──► [ MySQL (EF Core / Pomelo) ]
        │
        ▼ (REST + SignalR WebSockets autenticados via JWT)
[ WinForms Desktop - Teledesk.Desktop (Login JWT + Fila por Setor + Admin) ]
```

---

## 3. Como funciona Número Único vs. Número Dedicado por Setor na WhatsApp Cloud API (Meta)

Na **WhatsApp Cloud API (Meta)**:
- Uma conta comercial (**WABA** — *WhatsApp Business Account*) pode ter **um ou vários números** cadastrados.
- Cada número possui um identificador único chamado **`PhoneNumberId`**.
- Todo webhook enviado pela Meta informa qual número recebeu a mensagem em `entry[].changes[].value.metadata.phone_number_id`, além do identificador do cliente (`wa_id` / telefone em formato E.164).
- Ao responder pela Graph API, a API envia para `POST https://graph.facebook.com/{versao}/{PhoneNumberId}/messages`, garantindo que a resposta saia pelo **mesmo número** que o cliente contatou.

Com a relação entre **`NumeroWhatsApp`** e **`Setor`**, o mesmo modelo atende aos dois modos:

1. **Cenário A — Número Compartilhado (`1 NumeroWhatsApp -> N Setores`)**
   - O cliente envia mensagem para o número principal.
   - A API identifica o `PhoneNumberId` e verifica que existem **2 ou mais setores ativos** vinculados a ele.
   - O `ConversationService` inicia o atendimento em `EmUra` e responde com o menu dinâmico dos setores (ordenados por `OrdemUra`), ex.: `1 - Comercial`, `2 - Suporte`, `3 - Financeiro`.
   - Quando o cliente escolhe a opção, o atendimento muda para `AguardandoAtendente`, recebe o `SetorId` escolhido e é notificado no SignalR apenas para o grupo `setor-{setorId}`.

2. **Cenário B — Número Dedicado (`1 NumeroWhatsApp -> 1 Setor`)**
   - Um setor possui um número próprio cadastrado na tabela `NumeroWhatsApp`.
   - O cliente envia mensagem para esse número.
   - A API identifica o `PhoneNumberId`, encontra apenas **1 setor ativo** vinculado e **pula a URA**, criando o atendimento diretamente em `AguardandoAtendente` na fila daquele setor.

---

## 4. Evolução da Modelagem de Dados (MySQL / EF Core)

### Novas Entidades
- **`Setor`**
  - `Id` (`int`, PK)
  - `Nome` (`string`, obrigatório, máx. 100)
  - `Descricao` (`string?`, máx. 250)
  - `Ativo` (`bool`, padrão `true`)
  - `OrdemUra` (`int`, define a ordem de exibição na URA)
  - Navegações: `Atendentes` (N:N com `Atendente`), `NumerosWhatsApp` (N:N com `NumeroWhatsApp`), `Atendimentos` (1:N).

- **`NumeroWhatsApp`**
  - `Id` (`int`, PK)
  - `NomeIdentificacao` (`string`, obrigatório, ex.: *"Atendimento Geral"* ou *"WhatsApp Suporte"*)
  - `NumeroExibicao` (`string`, obrigatório, ex.: `"+55 11 99999-9999"`)
  - `PhoneNumberId` (`string`, obrigatório, índice único — ID oficial da Meta)
  - `Ativo` (`bool`, padrão `true`)
  - Navegações: `Setores` (N:N com `Setor`), `Atendimentos` (1:N).

### Atualizações nas Entidades Existentes
- **`Atendente`**
  - Substituir `Senha` (texto puro) por `SenhaHash` (`string`, obrigatório).
  - Adicionar `Perfil` (`PerfilAtendente` enum: `Atendente = 0`, `Admin = 1`).
  - Adicionar `Ativo` (`bool`, padrão `true`).
  - Adicionar navegação `Setores` (`ICollection<Setor>` — relacionamento N:N).

- **`Contato`**
  - Substituir `TelegramChatId` (`long`) por `WhatsAppId` (`string`, obrigatório, índice único, formato E.164).

- **`Atendimento`**
  - Adicionar `SetorId` (`int?`, FK opcional enquanto `EmUra` e preenchida ao entrar na fila do setor).
  - Adicionar `NumeroWhatsAppId` (`int`, FK obrigatória para responder pelo `PhoneNumberId` correto).
  - Adicionar controle de concorrência ao atribuir `AtendenteId` para impedir que dois atendentes assumam o mesmo chamado simultaneamente.

- **`Mensagem`**
  - Adicionar `WhatsAppMessageId` (`string?`, índice único no MySQL) para garantir **idempotência** caso a Meta reenvie o mesmo webhook.

---

## 5. Plano de Ação em Fases

### Fase 0 — Limpeza & Segurança (Saída do Telegram)
- [ ] Remover o pacote `Telegram.Bot` de `Teledesk.API.csproj`.
- [ ] Remover `TelegramPollingService.cs` e `BotController.cs`, e limpar registros/referências ao `ITelegramBotClient` em `Program.cs`, `ConversationService.cs` e `AtendimentoHub.cs`.
- [ ] **Segurança:** Revogar o token antigo do Telegram no `@BotFather` e removê-lo do `appsettings.json` versionado.
- [ ] Atualizar `Teledesk.API.http` removendo a rota de exemplo `/weatherforecast/`.
- **Critério de conclusão:** `dotnet build` da solution sem dependências ou referências ao Telegram e sem credenciais expostas no Git.

### Fase 1 — Evolução do Login para Autenticação Segura (JWT + Hash + Perfis)
- [ ] Criar enum `PerfilAtendente` (`Atendente`, `Admin`).
- [ ] Substituir `Atendente.Senha` por `SenhaHash` (usando `PasswordHasher<Atendente>` ou `BCrypt.Net-Next`), adicionar `Perfil` e `Ativo`, e gerar migration MySQL.
- [ ] Remover o auto-cadastro aberto no `AtendentesController` e criar seed inicial de usuário `Admin` em desenvolvimento.
- [ ] Implementar emissão de token **JWT Bearer** no login (claims: `NameIdentifier`, `Name`, `Email`, `Role`) e endpoint `GET /api/auth/me` (ou equivalente).
- [ ] Configurar `AddAuthentication().AddJwtBearer()` e `UseAuthentication()` antes de `UseAuthorization()` no `Program.cs`, protegendo controllers e o `AtendimentoHub` com `[Authorize]`.
- **Critério de conclusão:** Login retorna JWT válido, senhas ficam protegidas com hash no MySQL e requisições sem token são rejeitadas com `401`.

### Fase 2 — Modelagem Multi-Setor e Multi-Número no MySQL (EF Core + CRUDs Admin)
- [ ] Criar entidades `Setor` e `NumeroWhatsApp`.
- [ ] Atualizar `Contato` (`WhatsAppId`), `Atendimento` (`SetorId`, `NumeroWhatsAppId`, concorrência) e `Mensagem` (`WhatsAppMessageId`).
- [ ] Configurar Fluent API no `TeledeskDbContext` (relacionamentos N:N `Atendente <-> Setor` e `NumeroWhatsApp <-> Setor`, índices únicos em `PhoneNumberId`, `WhatsAppId` e `WhatsAppMessageId`).
- [ ] Gerar e aplicar nova migration no MySQL (`Pomelo`).
- [ ] Criar `SetoresController`, `NumerosWhatsAppController` e gestão de vínculos/usuários no `AtendentesController`, protegidos com `[Authorize(Roles = "Admin")]`.
- **Critério de conclusão:** Banco MySQL atualizado e administrador consegue cadastrar setores, atendentes e números vinculados via API.

### Fase 3 — Integração WhatsApp Cloud API (Oficial da Meta)
- [ ] Configurar aplicativo no Meta for Developers (WhatsApp Cloud API), número de teste e túnel HTTPS local (Dev Tunnels / ngrok) apontando para o servidor na máquina.
- [ ] Criar DTOs para deserialização do webhook da Meta e payload de envio de mensagens.
- [ ] Criar `IWhatsAppService` e `WhatsAppCloudService` usando `IHttpClientFactory`, enviando respostas pelo `PhoneNumberId` correspondente.
- [ ] Criar `WhatsAppWebhookController`:
  - `GET /api/webhook/whatsapp`: validação de `hub.mode`, `hub.verify_token` e retorno de `hub.challenge`.
  - `POST /api/webhook/whatsapp`: validação da assinatura (`X-Hub-Signature-256`) e envio para processamento.
- **Critério de conclusão:** Webhook validado pela Meta, recebendo mensagens reais no servidor local e respondendo pelo `PhoneNumberId` correto.

### Fase 4 — Evolução do `ConversationService` (URA Dinâmica + Fila por Setor)
- [ ] Adaptar `ConversationService` do Telegram para WhatsApp.
- [ ] Identificar `NumeroWhatsApp` pelo `PhoneNumberId` recebido no webhook e `Contato` pelo `wa_id`.
- [ ] Implementar roteamento:
  - Se `NumeroWhatsApp` tiver **1 setor** ativo: iniciar `Atendimento` já em `AguardandoAtendente` com `SetorId` preenchido.
  - Se `NumeroWhatsApp` tiver **2+ setores** ativos: iniciar em `EmUra`, montar menu dinâmico com os setores vinculados e processar a escolha do cliente.
- [ ] Persistir histórico completo na tabela `Mensagens` vinculado ao `Atendimento`.
- **Critério de conclusão:** Número compartilhado exibe URA dinâmica por setor e número dedicado cai direto na fila do setor configurado.

### Fase 5 — Evolução do `AtendimentoHub` (SignalR com Grupos por Setor + Concorrência)
- [ ] Proteger `AtendimentoHub` com `[Authorize]` e usar claims do JWT para identificar o atendente conectado.
- [ ] No `OnConnectedAsync`, consultar os setores do atendente e registrá-lo nos grupos `setor-{setorId}`.
- [ ] Substituir `Clients.All` por `Clients.Group($"setor-{setorId}")` nos eventos `NovaConversaNaFila`, `NovaMensagemRecebida`, `AtendimentoAssumido` e `AtendimentoFinalizado`.
- [ ] Filtrar `ObterFilaAtendimento` pelos setores do atendente logado.
- [ ] Atualizar `AssumirAtendimento`, `EnviarMensagemParaCliente` e `FinalizarAtendimento` para usar `IWhatsAppService` com o `PhoneNumberId` do atendimento e impedir que dois atendentes assumam o mesmo ticket.
- **Critério de conclusão:** Cada atendente visualiza e atende apenas a fila dos seus setores em tempo real, sem conflito de concorrência.

### Fase 6 — Evolução do `Teledesk.Desktop` (WinForms JWT + Multi-Setor + Admin)
- [ ] Atualizar `LoginForm` para autenticar via JWT e guardar token + perfil do atendente na sessão do aplicativo.
- [ ] Atualizar `Form1` para enviar o Bearer Token nas chamadas HTTP e na conexão `HubConnection` (`AccessTokenProvider`).
- [ ] Exibir o nome do setor na fila de atendimentos do `Form1`.
- [ ] Criar telas administrativas no WinForms (visíveis apenas para `PerfilAtendente.Admin`) para cadastrar setores, atendentes, números WhatsApp (`PhoneNumberId`) e gerenciar vínculos.
- **Critério de conclusão:** Fluxo completo ponta a ponta entre WhatsApp real, API local e múltiplos clientes WinForms logados em setores diferentes.

### Fase 7 — Resiliência, Idempotência e Preparação para Produção
- [ ] Garantir idempotência de webhooks verificando `WhatsAppMessageId` único antes de processar mensagens repetidas da Meta.
- [ ] Adicionar tratamento de erros/retries na chamada à Graph API e logs estruturados por `AtendimentoId` e `SetorId`.
- [ ] Documentar guia de publicação para sair da máquina local (VPS/Azure/Docker, HTTPS, variáveis de ambiente para JWT Secret, ConnectionString MySQL e Meta Access Token).
