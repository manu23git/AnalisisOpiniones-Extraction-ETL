namespace AnalisisOpiniones.Extraction.Infrastructure.Options;

public sealed class CsvSourceOptions
{
    public const string SectionName = "Sources:Csv";
    public string Path { get; set; } = "Data/surveys_part1.csv";
}
