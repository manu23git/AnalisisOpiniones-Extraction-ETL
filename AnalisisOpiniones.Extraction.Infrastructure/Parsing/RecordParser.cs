using System.Globalization;

namespace AnalisisOpiniones.Extraction.Infrastructure.Parsing;

internal static class RecordParser
{
    public static string Required(string? value, string field)
    {
        var cleaned = value?.Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new FormatException($"El campo {field} es obligatorio.");
        return cleaned;
    }

    public static int RequiredInt(string? value, string field)
    {
        if (!int.TryParse(Required(value, field), NumberStyles.None, CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"El campo {field} debe ser un entero.");
        return result;
    }

    public static int? OptionalInt(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"El campo {field} debe ser un entero.");
        return result;
    }

    public static byte? OptionalByte(string? value, string field, byte minimum, byte maximum)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!byte.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var result) ||
            result < minimum || result > maximum)
        {
            throw new FormatException($"El campo {field} debe estar entre {minimum} y {maximum}.");
        }
        return result;
    }

    public static DateOnly RequiredDate(string? value, string field)
    {
        if (!DateOnly.TryParseExact(
                Required(value, field),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            throw new FormatException($"El campo {field} debe usar el formato yyyy-MM-dd.");
        }
        return result;
    }
}
