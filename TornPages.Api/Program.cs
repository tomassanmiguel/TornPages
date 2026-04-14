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

// GET /state — returns current RenderState
app.MapGet("/state", (TornPagesEngine engine) =>
    Results.Ok(engine.GetState()));

// POST /action — applies a PlayerAction, returns updated RenderState
app.MapPost("/action", (PlayerAction action, TornPagesEngine engine) =>
{
    try
    {
        var state = engine.ApplyAction(action);
        return Results.Ok(state);
    }
    catch (EngineException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// GET /history/{pageIndex} — returns frozen RenderState for a historical page
app.MapGet("/history/{pageIndex:int}", (int pageIndex, TornPagesEngine engine) =>
    Results.Ok(engine.GetHistoricalState(pageIndex)));

app.Run();
