using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Domain.Models;
using AnalisisOpiniones.Extraction.Infrastructure.Extractors;
using AnalisisOpiniones.Extraction.Infrastructure.Options;
using AnalisisOpiniones.Extraction.Infrastructure.Staging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExtractionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CsvSourceOptions>()
            .Bind(configuration.GetSection(CsvSourceOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Path), "Debe configurar Sources:Csv:Path.")
            .ValidateOnStart();

        services.AddOptions<DatabaseSourceOptions>()
            .Bind(configuration.GetSection(DatabaseSourceOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Debe configurar Sources:Database:ConnectionString.")
            .ValidateOnStart();

        services.AddOptions<ApiSourceOptions>()
            .Bind(configuration.GetSection(ApiSourceOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "Sources:Api:BaseUrl debe ser una URL absoluta.")
            .Validate(options => options.TimeoutSeconds > 0, "El timeout de la API debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddOptions<StagingOptions>()
            .Bind(configuration.GetSection(StagingOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Debe configurar Staging:ConnectionString.")
            .ValidateOnStart();

        services.AddHttpClient(ApiSocialCommentExtractor.HttpClientName, (serviceProvider, client) =>
        {
            var apiOptions = serviceProvider.GetRequiredService<IOptions<ApiSourceOptions>>().Value;
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AnalisisOpiniones-ExtractionWorker/1.0");
        });

        services.AddSingleton<IExtractor<SurveyRecord>, SurveyCsvExtractor>();
        services.AddSingleton<IExtractor<WebReviewRecord>, DatabaseReviewExtractor>();
        services.AddSingleton<IExtractor<SocialCommentRecord>, ApiSocialCommentExtractor>();
        services.AddSingleton<IStagingRepository, SqlStagingRepository>();
        return services;
    }
}
