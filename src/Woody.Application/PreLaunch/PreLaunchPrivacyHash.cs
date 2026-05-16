using System.Security.Cryptography;
using System.Text;

namespace Woody.Application.PreLaunch;

public static class PreLaunchPrivacyHash
{
    /// <summary>SHA256 em hex minúsculo de (valor + segredo). Não armazena IP ou UA em claro.</summary>
    public static string Sha256Hex(string value, string secret)
    {
        var payload = Encoding.UTF8.GetBytes(value + secret);
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
