using System.Reflection;
using Woody.Application.DTOs;
using Woody.Application.Utilities;
using Woody.Application.Validation;
using Woody.Domain.Messaging;

namespace Woody.Application.Tests;

public class InputValidationTests
{
    [Fact]
    public void UpdateProfileRequestDto_DoesNotExposeRoleInput()
    {
        var roleProperty = typeof(UpdateProfileRequestDTO).GetProperty(
            "Role",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        Assert.Null(roleProperty);
    }

    [Fact]
    public void TryNormalizeHttpsImageUrl_RejectsTrackingUnsafeSchemes()
    {
        var ok = InputValidator.TryNormalizeHttpsImageUrl(
            "http://tracker.example/pixel.png",
            out var normalized,
            out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.Equal(InputValidator.InvalidImageUrlMessage, error);
    }

    [Fact]
    public void CreateCommunityRequestValidator_AppliesUrlPolicy()
    {
        var error = CreateCommunityRequestValidator.Validate(new CreateCommunityRequestDTO
        {
            Name = "Comunidade Segura",
            Description = "Descrição suficientemente longa",
            Category = "cultura",
            Visibility = "public",
            AvatarUrl = "data:image/png;base64,abc"
        });

        Assert.Equal(InputValidator.InvalidImageUrlMessage, error);
    }

    [Fact]
    public void CreateCommunityRequestValidator_RejectsOversizedPatchDescription()
    {
        var error = CreateCommunityRequestValidator.ValidatePatch(new CommunityUpdateRequestDTO
        {
            Description = new string('x', InputValidationLimits.CommunityDescriptionMaxLength + 1)
        });

        Assert.Equal("Descrição muito longa (máx. 2000 caracteres).", error);
    }

    [Theory]
    [InlineData("https://cdn.example.com/image.png", true)]
    [InlineData("http://cdn.example.com/image.png", false)]
    [InlineData("data:image/png;base64,abc", true)]
    [InlineData("data:image/svg+xml;base64,abc", false)]
    public void DirectMessageAttachmentPolicy_UsesHttpsOrSafeDataImageOnly(string url, bool expected)
    {
        Assert.Equal(expected, DirectMessageAttachmentPolicy.IsPermittedAttachmentUrl(url));
    }
}
