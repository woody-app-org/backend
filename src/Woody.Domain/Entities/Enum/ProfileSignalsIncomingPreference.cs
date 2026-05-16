namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Quem pode enviar sinais/flertes para esta utilizadora (destinatária).
/// </summary>
public enum ProfileSignalsIncomingPreference
{
    /// <summary>Todas as utilizadoras autenticadas (sujeito a bloqueios / cooldown).</summary>
    All = 0,

    /// <summary>Apenas utilizadoras que <em>esta utilizadora segue</em> (a destinatária segue a remetente).</summary>
    FollowingOnly = 1,

    /// <summary>Apenas utilizadoras que <em>seguem esta utilizadora</em> (a remetente segue a destinatária).</summary>
    FollowersOnly = 2,

    /// <summary>Ninguém pode enviar.</summary>
    Nobody = 3
}
