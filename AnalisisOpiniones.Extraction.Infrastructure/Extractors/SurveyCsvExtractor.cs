using System.Diagnostics;
using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;
using AnalisisOpiniones.Extraction.Infrastructure.Csv;
using AnalisisOpiniones.Extraction.Infrastructure.Options;
using AnalisisOpiniones.Extraction.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Infrastructure.Extractors;

public sealed class SurveyCsvExtractor(
    IOptions<CsvSourceOptions> options,
    ILogger<SurveyCsvExtractor> logger) : IExtractor<SurveyRecord>
{
    private static readonly string[] Headers =
    [
        "IdOpinion", "IdCliente", "IdProducto", "Fecha", "Comentario",
        "Clasificación", "PuntajeSatisfacción", "Fuente"
    ];

    public string SourceName => "CSV - Encuestas internas";

    public async Task<ExtractionBatch<SurveyRecord>> ExtractAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = ResolvePath(options.Value.Path);
        var rows = await new DelimitedFileReader().ReadAsync(path, Headers, cancellationToken);
        var records = new List<SurveyRecord>(rows.Count);
        var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = 0;

        foreach (var row in rows)
        {
            try
            {
                var identifier = RecordParser.Required(row["IdOpinion"], "IdOpinion");
                if (!identifiers.Add(identifier))
                    throw new FormatException($"IdOpinion duplicado: {identifier}.");

                records.Add(new SurveyRecord(
                    identifier,
                    RecordParser.OptionalInt(row["IdCliente"], "IdCliente"),
                    RecordParser.RequiredInt(row["IdProducto"], "IdProducto"),
                    RecordParser.RequiredDate(row["Fecha"], "Fecha"),
                    RecordParser.Required(row["Comentario"], "Comentario"),
                    string.IsNullOrWhiteSpace(row["Clasificación"])
                        ? null
                        : row["Clasificación"].Trim(),
                    RecordParser.OptionalByte(row["PuntajeSatisfacción"], "PuntajeSatisfacción", 1, 5),
                    RecordParser.Required(row["Fuente"], "Fuente")));
            }
            catch (FormatException exception)
            {
                rejected++;
                logger.LogWarning(
                    "CSV fila {RowNumber} rechazada: {Reason}",
                    row.RowNumber,
                    exception.Message);
            }
        }

        stopwatch.Stop();
        logger.LogInformation(
            "CSV leído desde {Path}: {Count} registros válidos y {Rejected} rechazados en {ElapsedMs} ms",
            path,
            records.Count,
            rejected,
            stopwatch.ElapsedMilliseconds);

        return new ExtractionBatch<SurveyRecord>(
            SourceName,
            records,
            rejected,
            stopwatch.Elapsed);
    }

    private static string ResolvePath(string configuredPath) =>
        Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
}
