namespace Woody.Infrastructure.Services.Email;

public class ResendOptions
{
    public string ApiKey { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string? FromName { get; set; }
}
