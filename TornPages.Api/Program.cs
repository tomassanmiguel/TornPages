using System.Text.Json;
using TornPages.Engine;

var builder = WebApplication.CreateBuilder(args);

// Single engine instance for the lifetime of the server
builder.Services.AddSingleton<TornPagesEngine>();

// CORS — origins configured in appsettings so they can vary by environment
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Local file used to persist the ping log across server restarts
var logPath = Path.Combine(AppContext.BaseDirectory, "ping-log.json");

// Append a ping entry to the local log file (fire-and-forget, non-blocking)
void AppendToLogFile(PingLogEntry entry)
{
    try
    {
        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
        File.AppendAllText(logPath, line);
    }
    catch
    {
        // Log write failure is non-fatal
    }
}

// GET /state — returns current RenderState
app.MapGet("/state", (TornPagesEngine engine) =>
    Results.Ok(engine.GetState()));

// POST /action — applies a PlayerAction, returns updated RenderState
app.MapPost("/action", (PlayerAction action, TornPagesEngine engine) =>
{
    try
    {
        var state = engine.ApplyAction(action);
        if (action.ActionType == "Ping")
        {
            var entry = engine.GetPingLog()[^1];
            AppendToLogFile(entry);
        }
        return Results.Ok(state);
    }
    catch (EngineException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// GET /pings — returns the in-memory ping log for this server session
app.MapGet("/pings", (TornPagesEngine engine) =>
    Results.Ok(engine.GetPingLog()));

// GET /history/{pageIndex} — returns frozen RenderState for a historical page
app.MapGet("/history/{pageIndex:int}", (int pageIndex, TornPagesEngine engine) =>
    Results.Ok(engine.GetHistoricalState(pageIndex)));

app.Run();
