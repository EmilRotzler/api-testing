using System.ComponentModel.DataAnnotations;

namespace ApiTesting.Common.Configuration;

public class EmailOptions
{
    public const string SectionName = "Email";

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string ReportRecipient { get; set; } = string.Empty;

    [Required]
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 25;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
}
