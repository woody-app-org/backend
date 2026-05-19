using System.Text.RegularExpressions;

namespace Woody.Application.Validation;

public static partial class CpfInputNormalizer
{
    private static readonly Regex NonDigitRegex = NonDigitPattern();

    public static string NormalizeDigits(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        return NonDigitRegex.Replace(cpf, string.Empty);
    }

    public static bool IsValid(string digits)
    {
        if (digits.Length != 11)
            return false;
        if (digits.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);
        var d1 = (sum * 10) % 11;
        if (d1 == 10) d1 = 0;
        if (d1 != digits[9] - '0') return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);
        var d2 = (sum * 10) % 11;
        if (d2 == 10) d2 = 0;
        return d2 == digits[10] - '0';
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitPattern();
}
