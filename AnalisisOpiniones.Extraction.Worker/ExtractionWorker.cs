using AnalisisOpiniones.Extraction.Application.Services;
using AnalisisOpiniones.Extraction.Worker.Options;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Worker;

public sealed class ExtractionWorker(
    ExtractionOrchestrator orchestrator,
    IOptions<WorkerOptions> options,
    IHostApplicationLifetime applicationLifetime,
    ILogger<ExtractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Worker de extracción iniciado. Modo={Mode}",
            options.Value.RunOnce ? "una ejecución" : "programado");

        do
        {
            try
            {
                await orchestrator.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "La extracción no pudo completarse");
                Environment.ExitCode = 1;

                if (options.Value.RunOnce)
                {
                    applicationLifetime.StopApplication();
                    return;
                }
            }

            if (options.Value.RunOnce)
            {
                applicationLifetime.StopApplication();
                return;
            }

            logger.LogInformation(
                "Próxima extracción programada dentro de {IntervalMinutes} minutos",
                options.Value.IntervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(options.Value.IntervalMinutes), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);

        logger.LogInformation("Worker de extracción detenido");
    }
}
