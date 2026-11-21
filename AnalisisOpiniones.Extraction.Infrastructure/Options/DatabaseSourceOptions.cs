namespace AnalisisOpiniones.Extraction.Infrastructure.Options;

public sealed class DatabaseSourceOptions
{
    public const string SectionName = "Sources:Database";
    public string ConnectionString { get; set; } = string.Empty;
}
