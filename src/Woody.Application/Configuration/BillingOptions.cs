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

    /// <summary>URL absoluta de sucesso (pode incluir <c>{CHECKOUT_SESSION_ID}</c> do Stripe).</summary>
    public string CheckoutSuccessUrl { get; set; } = string.Empty;

    /// <summary>URL absoluta se a utilizadora cancelar no checkout.</summary>
    public string CheckoutCancelUrl { get; set; } = string.Empty;

    /// <summary>URL absoluta para onde a Stripe redireciona após sair do Customer Billing Portal.</summary>
    public string CustomerPortalReturnUrl { get; set; } = string.Empty;

    /// <summary>Opcional: id <c>bpc_…</c> de configuração do portal na Stripe (produto/cancelamento/faturas).</summary>
    public string CustomerPortalConfigurationId { get; set; } = string.Empty;

    public StripePriceIdsOptions PriceIds { get; set; } = new();
}

public class StripePriceIdsOptions
{
    /// <summary>Price id do Stripe Billing para Pro mensal (ex.: price_…).</summary>
    public string ProMonthly { get; set; } = string.Empty;

    /// <summary>Price id do Stripe Billing para Pro anual (ex.: price_…).</summary>
    public string ProAnnual { get; set; } = string.Empty;
}
