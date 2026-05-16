namespace Woody.Application.DTOs.Billing;

/// <summary>Pedido de checkout; o servidor valida o código contra o catálogo e resolve o price id.</summary>
public class CreateBillingCheckoutRequestDto
{
    /// <summary>Ex.: <c>pro_monthly</c> — tem de existir no catálogo com price configurado.</summary>
    public string PlanCode { get; set; } = string.Empty;
}
