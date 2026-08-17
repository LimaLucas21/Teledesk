# 📞 Teledesk - Sistema de Atendimento Multi-Atendente integrado ao Telegram

Projeto prático de estudo end-to-end desenvolvido em **.NET 9**, **ASP.NET Core Web API**, **SignalR**, **Entity Framework Core** e **Windows Forms (WinForms)**.

---

## 🏗️ Arquitetura Geral

```text
[ Cliente (Telegram) ] 
       │
       ▼ (Webhook HTTP)
[ ASP.NET Core Web API ] ◄──► [ PostgreSQL / SQL Server (EF Core) ]
       │
       ▼ (SignalR WebSockets)
[ WinForms Desktop (Atendentes Humanos) ]
```

---

## 💻 Como Continuar / Rodar em uma Nova Máquina

1. **Clonar o repositório:**
   ```bash
   git clone https://github.com/LimaLucas21/Teledesk.git
   cd Teledesk
   ```

2. **Configurar as Credenciais Locais:**
   Crie o arquivo `Teledesk.API/appsettings.Development.json` (que fica ignorado pelo `.gitignore`) com seu token do Telegram:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "TelegramSettings": {
       "Token": "SEU_TOKEN_TELEGRAM_AQUI"
     }
   }
   ```

3. **Restaurar e Executar o Projeto:**
   ```bash
   dotnet restore
   dotnet run --project Teledesk.API/Teledesk.API.csproj
   ```

---

## 🗺️ Roadmap do Projeto

- [x] **Fase 1: Foundation & Integração Básica com Telegram**
  - [x] **Desafio 1.1:** Setup da Solution `.sln`, estrutura de pastas, configuração do `appsettings.json` e instalação do pacote `Telegram.Bot`.
  - [x] **Desafio 1.2:** Configuração da Injeção de Dependência (`ITelegramBotClient`) em `Program.cs` e criação do `BotController.cs`.
- [ ] **Fase 2: Modelagem de Dados & Persistência (EF Core)**
- [ ] **Fase 3: Motor de Estado da Conversa (URA vs Atendimento Humano)**
- [ ] **Fase 4: Servidor SignalR para Comunicação em Tempo Real**
- [ ] **Fase 5: Frontend Desktop com WinForms (Painel do Atendente)**
- [ ] **Fase 6: Loop Completo (Atendimento Humano Bidirecional)**
- [ ] **Fase 7: Resiliência, Refatoração, Logs e Defesa Técnica**

---

## 🎯 Desafio Atual: Desafio 1.2 (Fase 1)

### 📋 Tarefas a Implementar:

#### Task 1: Injeção de Dependência em `Teledesk.API/Program.cs`
- Ler o token da configuração (`builder.Configuration["TelegramSettings:Token"]`).
- Registrar o serviço `ITelegramBotClient` como **Singleton** no container de DI:
  ```csharp
  builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(token!));
  ```

#### Task 2: Criar o Controller `Teledesk.API/Controllers/BotController.cs`
- Criar a classe `BotController` decorada com `[ApiController]` e `[Route("api/[controller]")]`.
- Injetar `ITelegramBotClient` e `ILogger<BotController>` no construtor.
- Criar o método `[HttpPost]` recebendo `[FromBody] Update update`:
  - Verificar se a mensagem possui texto (`update.Message?.Text`).
  - Gravar no log usando `_logger.LogInformation(...)` informando o texto e a identificação do usuário.
  - Retornar `Ok()`.
