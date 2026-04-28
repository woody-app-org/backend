namespace Woody.Application.Configuration;

public class AuthSecurityOptions
{
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
