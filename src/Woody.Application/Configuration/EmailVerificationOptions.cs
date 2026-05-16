namespace Woody.Application.Configuration;

public class EmailVerificationOptions
{
    public int ExpirationMinutes { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
}
