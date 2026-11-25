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

    public class SimularMensagemWhatsAppDto
    {
        public string PhoneNumberId { get; set; } = "100000000000000";
        public string WaId { get; set; } = "5511999998888";
        public string NomeCliente { get; set; } = "Cliente Teste";
        public string Texto { get; set; } = "Olá";
    }
}

