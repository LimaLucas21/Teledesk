namespace Teledesk.API.Services.Interfaces
{
    public interface IWhatsAppService
    {
        Task<bool> EnviarMensagemTextoAsync(
            string phoneNumberId,
            string destinatarioWaId,
            string texto,
            string? customAccessToken = null,
            CancellationToken cancellationToken = default);
    }
}
