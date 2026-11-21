using AnalisisOpiniones.Extraction.Application.Services;
using AnalisisOpiniones.Extraction.Infrastructure;
using AnalisisOpiniones.Extraction.Worker;
using AnalisisOpiniones.Extraction.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
    .Validate(options => options.IntervalMinutes > 0, "Worker:IntervalMinutes debe ser mayor que cero.")
    .ValidateOnStart();

builder.Services.AddExtractionInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ExtractionOrchestrator>();
builder.Services.AddHostedService<ExtractionWorker>();

var host = builder.Build();
await host.RunAsync();
