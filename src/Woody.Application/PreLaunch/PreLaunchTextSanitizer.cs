using System.Text;

namespace Woody.Application.PreLaunch;

public static class PreLaunchTextSanitizer
{
    /// <summary>
    /// Remove caracteres de controle. Tab/CR/LF viram espaço; demais controles são removidos.
    /// </summary>
    public static string RemoveControlCharacters(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is '\t' or '\n' or '\r')
            {
                sb.Append(' ');
                continue;
            }

            if (char.IsControl(c))
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }
}
