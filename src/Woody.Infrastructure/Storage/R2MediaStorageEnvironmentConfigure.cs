using Microsoft.Extensions.Options;
using Woody.Application.Configuration;

namespace Woody.Infrastructure.Storage;

/// <summary>
/// Aplica variáveis de ambiente planas após o bind da secção <c>R2</c> (sobrescreve valores de appsettings).
/// </summary>
public sealed class R2MediaStorageEnvironmentConfigure : IPostConfigureOptions<R2MediaStorageOptions>
{
    public void PostConfigure(string? name, R2MediaStorageOptions o)
    {
        Apply("R2_ACCOUNT_ID", v => o.AccountId = v);
        Apply("R2_ACCESS_KEY_ID", v => o.AccessKeyId = v);
        Apply("R2_SECRET_ACCESS_KEY", v => o.SecretAccessKey = v);
        Apply("R2_BUCKET", v => o.Bucket = v);
        Apply("R2_PUBLIC_BASE_URL", v => o.PublicBaseUrl = v);
        Apply("R2_SERVICE_URL", v => o.ServiceUrl = v);
        Apply("R2_REGION", v => o.Region = v);
    }

    private static void Apply(string envName, Action<string> set)
    {
        var v = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(v))
            set(v.Trim());
    }
}
