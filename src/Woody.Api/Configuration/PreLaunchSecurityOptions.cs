namespace Woody.Api.Configuration;

public sealed class PreLaunchSecurityOptions
{
    public const string SectionName = "PreLaunch";

    /// <summary>Segredo para SHA256(IP+secret) e SHA256(UA+secret). Env: PRELAUNCH_HASH_SECRET.</summary>
    public string HashSecret { get; set; } = "";
}
