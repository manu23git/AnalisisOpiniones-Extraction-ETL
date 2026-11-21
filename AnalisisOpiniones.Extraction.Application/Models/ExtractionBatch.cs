namespace AnalisisOpiniones.Extraction.Application.Models;

public sealed record ExtractionBatch<TRecord>(
    string Source,
    IReadOnlyList<TRecord> Records,
    int RejectedRecords,
    TimeSpan Duration);
