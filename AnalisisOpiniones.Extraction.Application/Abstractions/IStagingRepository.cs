using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;

namespace AnalisisOpiniones.Extraction.Application.Abstractions;

public interface IStagingRepository
{
    Task<long> StartRunAsync(CancellationToken cancellationToken = default);

    Task<StagingWriteResult> SaveSurveysAsync(
        long runId,
        ExtractionBatch<SurveyRecord> batch,
        CancellationToken cancellationToken = default);

    Task<StagingWriteResult> SaveWebReviewsAsync(
        long runId,
        ExtractionBatch<WebReviewRecord> batch,
        CancellationToken cancellationToken = default);

    Task<StagingWriteResult> SaveSocialCommentsAsync(
        long runId,
        ExtractionBatch<SocialCommentRecord> batch,
        CancellationToken cancellationToken = default);

    Task CompleteRunAsync(
        long runId,
        ExtractionRunSummary summary,
        CancellationToken cancellationToken = default);

    Task FailRunAsync(
        long runId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
