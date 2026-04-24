namespace Woody.Application.DTOs.Billing;

/// <summary>Pedido de checkout premium da comunidade; o servidor fixa o <c>planCode</c> e o price id Stripe.</summary>
public sealed class CreateCommunityPremiumCheckoutRequestDto
{
    public int CommunityId { get; set; }
}
