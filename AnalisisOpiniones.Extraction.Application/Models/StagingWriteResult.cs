namespace AnalisisOpiniones.Extraction.Application.Models;

public sealed record StagingWriteResult(
    string Source,
    int Extracted,
    int Inserted,
    int Duplicates,
    int Rejected,
    TimeSpan ExtractionDuration);
