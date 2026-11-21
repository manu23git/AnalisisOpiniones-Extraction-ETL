using System.Diagnostics;
using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.Extraction.Application.Services;

public sealed class ExtractionOrchestrator(
    IExtractor<SurveyRecord> surveyExtractor,
    IExtractor<WebReviewRecord> webReviewExtractor,
    IExtractor<SocialCommentRecord> socialCommentExtractor,
    IStagingRepository stagingRepository,
    ILogger<ExtractionOrchestrator> logger)
{
    public async Task<ExtractionRunSummary> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var runId = await stagingRepository.StartRunAsync(cancellationToken);

        logger.LogInformation(
            "Iniciando ejecución de extracción {RunId} para CSV, base relacional y API REST",
            runId);

        try
        {
            var surveyTask = surveyExtractor.ExtractAsync(cancellationToken);
            var webReviewTask = webReviewExtractor.ExtractAsync(cancellationToken);
            var socialCommentTask = socialCommentExtractor.ExtractAsync(cancellationToken);

            await Task.WhenAll(surveyTask, webReviewTask, socialCommentTask);

            var surveyBatch = await surveyTask;
            var webReviewBatch = await webReviewTask;
            var socialCommentBatch = await socialCommentTask;

            var surveyWriteTask = stagingRepository.SaveSurveysAsync(
                runId,
                surveyBatch,
                cancellationToken);
            var webReviewWriteTask = stagingRepository.SaveWebReviewsAsync(
                runId,
                webReviewBatch,
                cancellationToken);
            var socialCommentWriteTask = stagingRepository.SaveSocialCommentsAsync(
                runId,
                socialCommentBatch,
                cancellationToken);

            await Task.WhenAll(surveyWriteTask, webReviewWriteTask, socialCommentWriteTask);

            var sources = new[]
            {
                await surveyWriteTask,
                await webReviewWriteTask,
                await socialCommentWriteTask
            };

            stopwatch.Stop();
            var summary = new ExtractionRunSummary(
                runId,
                startedAt,
                DateTimeOffset.UtcNow,
                sources);

            await stagingRepository.CompleteRunAsync(runId, summary, cancellationToken);
            LogSummary(summary, stopwatch.Elapsed);
            return summary;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "La ejecución de extracción {RunId} falló después de {ElapsedMs} ms",
                runId,
                stopwatch.ElapsedMilliseconds);

            await stagingRepository.FailRunAsync(
                runId,
                exception.Message,
                CancellationToken.None);
            throw;
        }
    }

    private void LogSummary(ExtractionRunSummary summary, TimeSpan elapsed)
    {
        foreach (var source in summary.Sources)
        {
            logger.LogInformation(
                "Fuente {Source}: extraídos={Extracted}, insertados={Inserted}, duplicados={Duplicates}, rechazados={Rejected}, duración={DurationMs} ms",
                source.Source,
                source.Extracted,
                source.Inserted,
                source.Duplicates,
                source.Rejected,
                source.ExtractionDuration.TotalMilliseconds);
        }

        logger.LogInformation(
            "Extracción {RunId} completada en {ElapsedMs} ms. Total extraídos={Extracted}, insertados={Inserted}, duplicados={Duplicates}, rechazados={Rejected}",
            summary.RunId,
            elapsed.TotalMilliseconds,
            summary.TotalExtracted,
            summary.TotalInserted,
            summary.TotalDuplicates,
            summary.TotalRejected);
    }
}
