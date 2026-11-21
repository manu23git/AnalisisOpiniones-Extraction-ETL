namespace AnalisisOpiniones.Extraction.Infrastructure.Options;

public sealed class StagingOptions
{
    public const string SectionName = "Staging";
    public string ConnectionString { get; set; } = string.Empty;
}
