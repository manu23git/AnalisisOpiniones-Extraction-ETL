using System.Diagnostics;
using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;
using AnalisisOpiniones.Extraction.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Infrastructure.Extractors;

public sealed class DatabaseReviewExtractor(
    IOptions<DatabaseSourceOptions> options,
    ILogger<DatabaseReviewExtractor> logger) : IExtractor<WebReviewRecord>
{
    public string SourceName => "SQL Server - Reseñas web";

    public async Task<ExtractionBatch<WebReviewRecord>> ExtractAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var records = new List<WebReviewRecord>();
        const string query = """
            SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating
            FROM dbo.web_reviews
            ORDER BY IdReview;
            """;

        await using var connection = new SqlConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(query, connection) { CommandTimeout = 60 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new WebReviewRecord(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetInt32(1),
                reader.GetInt32(2),
                DateOnly.FromDateTime(reader.GetDateTime(3)),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetByte(5)));
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Base relacional consultada: {Count} reseñas extraídas en {ElapsedMs} ms",
            records.Count,
            stopwatch.ElapsedMilliseconds);

        return new ExtractionBatch<WebReviewRecord>(
            SourceName,
            records,
            0,
            stopwatch.Elapsed);
    }
}
