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

        Task<string?> ResolverLidParaTelefoneAsync(
            string session,
            string lid,
            string? customAccessToken = null,
            CancellationToken cancellationToken = default);
    }
}
