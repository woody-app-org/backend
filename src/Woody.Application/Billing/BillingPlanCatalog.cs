using Woody.Application.Configuration;

namespace Woody.Application.Billing;

/// <summary>Mapeia códigos de plano internos para recursos Stripe (nunca confiar no preço enviado pelo cliente).</summary>
public static class BillingPlanCatalog
{
    public static bool TryResolveStripePriceId(BillingOptions options, string planCode, out string priceId)
    {
        priceId = string.Empty;
        if (string.IsNullOrWhiteSpace(planCode))
            return false;

        var t = planCode.Trim();
        if (string.Equals(t, BillingPlanCodes.ProMonthly, StringComparison.Ordinal))
        {
            priceId = options.Stripe?.PriceIds?.ProMonthly?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(priceId);
        }

        if (string.Equals(t, BillingPlanCodes.ProAnnual, StringComparison.Ordinal))
        {
            priceId = options.Stripe?.PriceIds?.ProAnnual?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(priceId);
        }

        return false;
    }

    public static bool IsKnownCheckoutPlanCode(string planCode)
    {
        var t = planCode?.Trim();
        if (string.IsNullOrEmpty(t)) return false;
        return string.Equals(t, BillingPlanCodes.ProMonthly, StringComparison.Ordinal)
               || string.Equals(t, BillingPlanCodes.ProAnnual, StringComparison.Ordinal);
    }
}
