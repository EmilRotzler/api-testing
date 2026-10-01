namespace ApiTesting.Core.Models;

public record EmailMessage(
    string To,
    string Subject,
    string Body,
    IReadOnlyList<EmailAttachment> Attachments
);

public record EmailAttachment(string FileName, string ContentType, byte[] Content);
