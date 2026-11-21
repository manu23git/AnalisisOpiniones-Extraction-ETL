namespace AnalisisOpiniones.Extraction.Worker.Options;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";
    public bool RunOnce { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
}
