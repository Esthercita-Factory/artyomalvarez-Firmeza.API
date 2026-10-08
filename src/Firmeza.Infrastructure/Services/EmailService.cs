using Firmeza.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Firmeza.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        byte[]? attachmentBytes = null,
        string? attachmentName = null,
        CancellationToken cancellationToken = default)
    {
        var smtpHost = _config["Smtp:Host"];
        var smtpPortStr = _config["Smtp:Port"];
        var smtpUser = _config["Smtp:User"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:From"] ?? "notificaciones@firmeza.com";

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("Configuración SMTP no encontrada en appsettings. Simulación de correo a {To} con asunto '{Subject}'.", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Firmeza Notificaciones", fromEmail));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentName))
        {
            builder.Attachments.Add(attachmentName, attachmentBytes, ContentType.Parse("application/pdf"));
        }

        message.Body = builder.ToMessageBody();

        int port = int.TryParse(smtpPortStr, out var p) ? p : 587;

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
            {
                await client.AuthenticateAsync(smtpUser, smtpPass, cancellationToken);
            }
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Correo enviado exitosamente a {To}.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo electrónico a {To}.", to);
            throw;
        }
    }
}
