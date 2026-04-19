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

        if (string.Equals(planCode.Trim(), BillingPlanCodes.ProMonthly, StringComparison.Ordinal))
        {
            priceId = options.Stripe?.PriceIds?.ProMonthly?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(priceId);
        }

        return false;
    }

    public static bool IsKnownCheckoutPlanCode(string planCode) =>
        string.Equals(planCode?.Trim(), BillingPlanCodes.ProMonthly, StringComparison.Ordinal);
}
