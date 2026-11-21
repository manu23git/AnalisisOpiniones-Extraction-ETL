using AnalisisOpiniones.Extraction.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SocialCommentsCatalog>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "social-comments-api",
    utcTime = DateTimeOffset.UtcNow
}));

app.MapGet("/api/comments", async (
    SocialCommentsCatalog catalog,
    CancellationToken cancellationToken) =>
{
    var comments = await catalog.GetAllAsync(cancellationToken);
    return Results.Ok(comments);
});

await app.RunAsync();
