namespace Woody.Application.Configuration;

/// <summary>
/// Configuração de billing (segredos via env / user secrets; ids de preço para mapear checkout Stripe → plano interno).
/// </summary>
public class BillingOptions
{
    public StripeBillingOptions Stripe { get; set; } = new();
}

public class StripeBillingOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public StripePriceIdsOptions PriceIds { get; set; } = new();
}

public class StripePriceIdsOptions
{
    /// <summary>Price id do Stripe Billing para Pro mensal (ex.: price_…).</summary>
    public string ProMonthly { get; set; } = string.Empty;
}
