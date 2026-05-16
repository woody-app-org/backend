namespace Woody.Application.DTOs;

public class UpdateInterestsRequestDTO
{
    public List<InterestItemDto> Interests { get; set; } = new();
}
