using System.Globalization;
using System.Text;
using AnalisisOpiniones.Extraction.Domain.Models;
using Microsoft.VisualBasic.FileIO;

namespace AnalisisOpiniones.Extraction.Api.Services;

public sealed class SocialCommentsCatalog(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<SocialCommentsCatalog> logger)
{
    private IReadOnlyList<SocialCommentRecord>? _cachedRecords;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<SocialCommentRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedRecords is not null)
            return _cachedRecords;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedRecords is not null)
                return _cachedRecords;

            var configuredPath = configuration["SocialCommentsFile:Path"]
                                 ?? "../Data/social_comments.csv";
            var path = Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(configuredPath, environment.ContentRootPath);
            _cachedRecords = ReadFile(path);
            logger.LogInformation(
                "Catálogo REST preparado con {Count} comentarios desde {Path}",
                _cachedRecords.Count,
                path);
            return _cachedRecords;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static IReadOnlyList<SocialCommentRecord> ReadFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el archivo de comentarios para la API.", path);

        using var parser = new TextFieldParser(path, new UTF8Encoding(false, true))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        var headers = parser.ReadFields()
                      ?? throw new InvalidDataException("El archivo de comentarios no tiene encabezado.");
        var expected = new[] { "IdComment", "IdCliente", "IdProducto", "Fuente", "Fecha", "Comentario" };
        if (!headers.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("El encabezado de social_comments.csv no tiene la estructura esperada.");

        var records = new List<SocialCommentRecord>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length == 0)
                continue;
            if (fields.Length != expected.Length)
                throw new InvalidDataException("Una fila de social_comments.csv tiene una cantidad de columnas inválida.");

            records.Add(new SocialCommentRecord(
                Required(fields[0], "IdComment"),
                ParseIdentifier(fields[1], 'C', false),
                ParseIdentifier(fields[2], 'P', true)
                    ?? throw new InvalidDataException("IdProducto es obligatorio."),
                Required(fields[3], "Fuente"),
                ParseDate(fields[4]),
                Required(fields[5], "Comentario")));
        }

        return records;
    }

    private static int? ParseIdentifier(string? value, char prefix, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
                throw new InvalidDataException($"El identificador con prefijo {prefix} es obligatorio.");
            return null;
        }

        var cleaned = value.Trim();
        if (cleaned.Length < 2 || char.ToUpperInvariant(cleaned[0]) != prefix ||
            !int.TryParse(cleaned[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var identifier))
        {
            throw new InvalidDataException($"El identificador '{value}' no tiene el formato esperado.");
        }
        return identifier;
    }

    private static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw new InvalidDataException($"La fecha '{value}' no tiene el formato yyyy-MM-dd.");
        }
        return date;
    }

    private static string Required(string? value, string field)
    {
        var cleaned = value?.Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new InvalidDataException($"El campo {field} es obligatorio.");
        return cleaned;
    }
}
