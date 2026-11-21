namespace AnalisisOpiniones.Extraction.Application.Models;

public sealed record ExtractionRunSummary(
    long RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    IReadOnlyList<StagingWriteResult> Sources)
{
    public TimeSpan TotalDuration => FinishedAt - StartedAt;
    public int TotalExtracted => Sources.Sum(source => source.Extracted);
    public int TotalInserted => Sources.Sum(source => source.Inserted);
    public int TotalDuplicates => Sources.Sum(source => source.Duplicates);
    public int TotalRejected => Sources.Sum(source => source.Rejected);
}
