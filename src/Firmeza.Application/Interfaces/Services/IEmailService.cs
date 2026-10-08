namespace Firmeza.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentName = null, CancellationToken cancellationToken = default);
}
