using System.ComponentModel.DataAnnotations;

namespace ApiTesting.Common.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    [Range(1, 8760)]
    public int TokenLifetimeHours { get; set; } = 24;
}
