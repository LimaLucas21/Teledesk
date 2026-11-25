using System.Text.Json.Serialization;

namespace Teledesk.API.DTOs
{
    public class WhatsAppWebhookPayload
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("entry")]
        public List<WhatsAppEntryDto> Entry { get; set; } = new();
    }

    public class WhatsAppEntryDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("changes")]
        public List<WhatsAppChangeDto> Changes { get; set; } = new();
    }

    public class WhatsAppChangeDto
    {
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("value")]
        public WhatsAppChangeValueDto? Value { get; set; }
    }

    public class WhatsAppChangeValueDto
    {
        [JsonPropertyName("messaging_product")]
        public string? MessagingProduct { get; set; }

        [JsonPropertyName("metadata")]
        public WhatsAppMetadataDto? Metadata { get; set; }

        [JsonPropertyName("contacts")]
        public List<WhatsAppContactDto> Contacts { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<WhatsAppIncomingMessageDto> Messages { get; set; } = new();
    }

    public class WhatsAppMetadataDto
    {
        [JsonPropertyName("display_phone_number")]
        public string? DisplayPhoneNumber { get; set; }

        [JsonPropertyName("phone_number_id")]
        public string? PhoneNumberId { get; set; }
    }

    public class WhatsAppContactDto
    {
        [JsonPropertyName("profile")]
        public WhatsAppProfileDto? Profile { get; set; }

        [JsonPropertyName("wa_id")]
        public string? WaId { get; set; }
    }

    public class WhatsAppProfileDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class WhatsAppIncomingMessageDto
    {
        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public WhatsAppTextDto? Text { get; set; }
    }

    public class WhatsAppTextDto
    {
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

    // --- DTOs do WAHA (https://waha.devlike.pro/swagger/) ---

    public class WahaWebhookPayload
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("session")]
        public string? Session { get; set; }

        [JsonPropertyName("me")]
        public WahaMeDto? Me { get; set; }

        [JsonPropertyName("payload")]
        public WahaMessagePayloadDto? Payload { get; set; }
    }

    public class WahaMeDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("pushName")]
        public string? PushName { get; set; }
    }

    public class WahaMessagePayloadDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("chatId")]
        public string? ChatId { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("fromMe")]
        public bool FromMe { get; set; }

        [JsonPropertyName("notifyName")]
        public string? NotifyName { get; set; }

        [JsonPropertyName("participant")]
        public string? Participant { get; set; }

        [JsonPropertyName("_data")]
        public WahaRawDataDto? Data { get; set; }
    }

    public class WahaRawDataDto
    {
        [JsonPropertyName("notifyName")]
        public string? NotifyName { get; set; }

        [JsonPropertyName("pushName")]
        public string? PushName { get; set; }

        [JsonPropertyName("key")]
        public WahaMessageKeyDto? Key { get; set; }

        [JsonPropertyName("Info")]
        public WahaGowsInfoDto? Info { get; set; }
    }

    public class WahaGowsInfoDto
    {
        [JsonPropertyName("PushName")]
        public string? PushName { get; set; }

        [JsonPropertyName("SenderAlt")]
        public string? SenderAlt { get; set; }
    }

    public class WahaMessageKeyDto
    {
        [JsonPropertyName("remoteJid")]
        public string? RemoteJid { get; set; }

        [JsonPropertyName("remoteJidAlt")]
        public string? RemoteJidAlt { get; set; }

        [JsonPropertyName("participantAlt")]
        public string? ParticipantAlt { get; set; }

        [JsonPropertyName("senderPn")]
        public string? SenderPn { get; set; }
    }

    public class SimularMensagemWhatsAppDto
    {
        public string PhoneNumberId { get; set; } = "default";
        public string WaId { get; set; } = "5511999998888";
        public string NomeCliente { get; set; } = "Cliente Teste";
        public string Texto { get; set; } = "Olá";
    }
}
