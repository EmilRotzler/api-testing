using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
