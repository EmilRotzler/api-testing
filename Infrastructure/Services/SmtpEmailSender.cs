using ApiTesting.Common.Configuration;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ApiTesting.Infrastructure.Services;

public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var mimeMessage = BuildMimeMessage(settings.From, message);

        try
        {
            using var client = new SmtpClient();
            // Auto picks implicit SSL on 465 and STARTTLS elsewhere; None is for local catchers like smtp4dev.
            var socketOptions = settings.Smtp.UseSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.None;
            await client.ConnectAsync(
                settings.Smtp.Host,
                settings.Smtp.Port,
                socketOptions,
                cancellationToken
            );

            if (!string.IsNullOrEmpty(settings.Smtp.Username))
            {
                await client.AuthenticateAsync(
                    settings.Smtp.Username,
                    settings.Smtp.Password ?? string.Empty,
                    cancellationToken
                );
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.EmailSendFailed(ex, message.Subject);
            throw;
        }

        logger.EmailSent(message.Subject);
    }

    private static MimeMessage BuildMimeMessage(string from, EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(from));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var body = new BodyBuilder { TextBody = message.Body };
        foreach (var attachment in message.Attachments)
        {
            body.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType)
            );
        }

        mimeMessage.Body = body.ToMessageBody();
        return mimeMessage;
    }
}
