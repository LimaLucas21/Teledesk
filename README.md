# 📞 Teledesk - Sistema de Atendimento Multi-Atendente integrado ao Telegram

Projeto prático de estudo end-to-end desenvolvido em **.NET 9**, **ASP.NET Core Web API**, **SignalR**, **Entity Framework Core (MySQL)** e **Windows Forms (WinForms)**.

---

## 🏗️ Arquitetura Geral

```text
[ Cliente (Telegram) ] 
       │
       ▼ (Webhook HTTP)
[ ASP.NET Core Web API ] ◄──► [ MySQL (EF Core / Pomelo) ]
       │
       ▼ (SignalR WebSockets / Push)
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
   Crie o arquivo `Teledesk.API/appsettings.Development.json` (que fica ignorado pelo `.gitignore`) com seu token do Telegram e a string de conexão do MySQL:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=teledesk_db;Uid=root;Pwd=sua_senha;"
     },
     "TelegramSettings": {
       "Token": "SEU_TOKEN_TELEGRAM_AQUI"
     }
   }
   ```

3. **Restaurar, Aplicar Migrations e Executar o Projeto:**
   ```bash
   dotnet restore
   dotnet ef database update --project Teledesk.API
   dotnet run --project Teledesk.API/Teledesk.API.csproj
   ```

---

## 🗺️ Roadmap do Projeto

- [x] **Fase 1: Foundation & Integração Básica com Telegram**
  - [x] **Desafio 1.1:** Setup da Solution `.sln`, estrutura de pastas, configuração do `appsettings.json` e instalação do pacote `Telegram.Bot`.
  - [x] **Desafio 1.2:** Configuração da Injeção de Dependência (`ITelegramBotClient`) em `Program.cs` e criação do `BotController.cs`.
- [x] **Fase 2: Modelagem de Dados & Persistência (EF Core com MySQL)**
  - [x] **Desafio 2.1:** Criação dos modelos de domínio (`Contato`, `Atendente`, `Atendimento`, `Mensagem` e Enums).
  - [x] **Desafio 2.2:** Instalação do EF Core MySQL (Pomelo) e criação do `TeledeskDbContext` com Fluent API.
  - [x] **Desafio 2.3:** Geração de Migrations (`InitialCreateMySql`) para MySQL.
- [x] **Fase 3: Motor de Estado da Conversa (URA vs Atendimento Humano)**
  - [x] **Desafio 3.1:** Criação do `IConversationService` e `ConversationService`.
  - [x] **Desafio 3.2:** Busca/Cadastro automático de `Contato`, criação de `Atendimento` ativo e persistência de mensagens.
  - [x] **Desafio 3.3:** Máquina de estados da URA com menu interativo e transbordo para fila humana (`StatusAtendimento.AguardandoAtendente`).
- [x] **Fase 4: Servidor SignalR para Comunicação em Tempo Real**
  - [x] **Desafio 4.1:** Criação dos DTOs desacoplados (`AtendimentoDto`, `MensagemDto`).
  - [x] **Desafio 4.2:** Criação do `AtendimentoHub` com métodos bidirecionais (`AssumirAtendimento`, `EnviarMensagemParaCliente`, `FinalizarAtendimento`, `ObterFilaAtendimento`, `ObterHistoricoMensagens`).
  - [x] **Desafio 4.3:** Integração do `IHubContext<AtendimentoHub>` no `ConversationService` emitindo eventos em tempo real (`NovaConversaNaFila`, `NovaMensagemRecebida`).
  - [x] **Desafio 4.4:** Mapeamento do endpoint `/hubs/atendimento` e política de CORS no `Program.cs`.
- [ ] **Fase 5: Frontend Desktop com WinForms (Painel do Atendente)**
- [ ] **Fase 6: Loop Completo (Atendimento Humano Bidirecional)**
- [ ] **Fase 7: Resiliência, Refatoração, Logs e Defesa Técnica**

---

## 🎯 Próximo Desafio: Fase 5 (Frontend Desktop com WinForms)

Na Fase 5, criaremos a aplicação Windows Forms para os operadores:
1. Conectar ao Hub do SignalR (`/hubs/atendimento`).
2. Listar em tempo real a fila de atendimentos pendentes.
3. Permitir que o operador assuma um atendimento, visualize o histórico e converse em tempo real com o cliente do Telegram.
