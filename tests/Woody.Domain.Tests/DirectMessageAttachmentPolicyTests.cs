using Woody.Domain.Messaging;

namespace Woody.Domain.Tests;

public sealed class DirectMessageAttachmentPolicyTests
{
    [Theory]
    [InlineData("https://cdn.example.com/a.png")]
    [InlineData("HTTP://LOCALHOST/img.jpg")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("data:image/jpeg;charset=utf-8;base64,AAAA")]
    [InlineData("data:image/jpg;base64,AAAA")]
    [InlineData("data:image/gif;base64,R0lGODlhAQABAAAAACw=")]
    [InlineData("data:image/webp;base64,UklGRiIAAABXRUJQ")]
    public void IsPermittedAttachmentUrl_accepts_safe_urls(string url) =>
        Assert.True(DirectMessageAttachmentPolicy.IsPermittedAttachmentUrl(url));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("data:image/svg+xml;base64,PHN2Zy8+")]
    [InlineData("data:image/png,AAAA")] // sem base64
    [InlineData("file:///etc/passwd")]
    [InlineData("vbscript:msgbox")]
    [InlineData("data:image/png;base64\n,evil")]
    public void IsPermittedAttachmentUrl_rejects_unsafe_or_invalid(string url) =>
        Assert.False(DirectMessageAttachmentPolicy.IsPermittedAttachmentUrl(url));
}
