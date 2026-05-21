using System.Security.Cryptography;

namespace Woody.Application.Posts;

/// <summary>Gera identificadores públicos opacos para posts (<c>pst_</c> + 12 chars alfanuméricos).</summary>
public static class PostPublicIdGenerator
{
    public const string Prefix = "pst_";
    public const int MaxLength = 16;

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int RandomPartLength = 12;

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[RandomPartLength];
        Span<byte> bytes = stackalloc byte[RandomPartLength];
        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < RandomPartLength; i++)
            buffer[i] = Alphabet[bytes[i] % Alphabet.Length];
        return Prefix + new string(buffer);
    }
}
