namespace AnalisisOpiniones.Extraction.Infrastructure.Options;

public sealed class ApiSourceOptions
{
    public const string SectionName = "Sources:Api";
    public string BaseUrl { get; set; } = "http://localhost:5084";
    public string Endpoint { get; set; } = "/api/comments";
    public int TimeoutSeconds { get; set; } = 30;
}
