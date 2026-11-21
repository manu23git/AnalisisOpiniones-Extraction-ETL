using System.Diagnostics;
using System.Net.Http.Json;
using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;
using AnalisisOpiniones.Extraction.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Infrastructure.Extractors;

public sealed class ApiSocialCommentExtractor(
    IHttpClientFactory httpClientFactory,
    IOptions<ApiSourceOptions> options,
    ILogger<ApiSocialCommentExtractor> logger) : IExtractor<SocialCommentRecord>
{
    public const string HttpClientName = "SocialCommentsApi";
    public string SourceName => "API REST - Comentarios sociales";

    public async Task<ExtractionBatch<SocialCommentRecord>> ExtractAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(options.Value.Endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseRecords = await response.Content.ReadFromJsonAsync<List<SocialCommentRecord>>(
                                  cancellationToken: cancellationToken)
                              ?? [];
        var records = new List<SocialCommentRecord>(responseRecords.Count);
        var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = 0;

        foreach (var record in responseRecords)
        {
            if (string.IsNullOrWhiteSpace(record.IdComment) ||
                record.IdProducto <= 0 ||
                string.IsNullOrWhiteSpace(record.Comentario) ||
                string.IsNullOrWhiteSpace(record.Fuente) ||
                !identifiers.Add(record.IdComment))
            {
                rejected++;
                logger.LogWarning(
                    "Comentario de API rechazado por campos inválidos o duplicados: {IdComment}",
                    record.IdComment);
                continue;
            }

            records.Add(record);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "API REST consultada: {Count} comentarios válidos y {Rejected} rechazados en {ElapsedMs} ms",
            records.Count,
            rejected,
            stopwatch.ElapsedMilliseconds);

        return new ExtractionBatch<SocialCommentRecord>(
            SourceName,
            records,
            rejected,
            stopwatch.Elapsed);
    }
}
