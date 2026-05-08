using System.Text.Json;

namespace Woody.Application.Helpers;

/// <summary>
/// Utilitário para manipulação do DecisionLog de verificações de identidade.
/// O log é um array JSON com entradas { action, by, at } — sem storageKey ou dados sensíveis.
/// </summary>
public static class VerificationDecisionLogHelper
{
    public static string Append(string? existingLog, string action, int actorUserId, DateTime at)
    {
        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(existingLog))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(existingLog);
                if (parsed != null)
                    events.AddRange(parsed.Cast<object>());
            }
            catch
            {
                // Log corrompido: inicia novo
            }
        }

        events.Add(new
        {
            action,
            by = actorUserId,
            at = at.ToUniversalTime().ToString("o")
        });

        return JsonSerializer.Serialize(events);
    }
}
