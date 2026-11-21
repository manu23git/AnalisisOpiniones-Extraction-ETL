using AnalisisOpiniones.Extraction.Application.Models;

namespace AnalisisOpiniones.Extraction.Application.Abstractions;

public interface IExtractor<TRecord>
{
    string SourceName { get; }
    Task<ExtractionBatch<TRecord>> ExtractAsync(CancellationToken cancellationToken = default);
}
