using System.Text.Json;
using System.Text.Json.Serialization;
using TornPages.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TornPagesEngine>();

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// CORS
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

// ─── Profile list ──────────────────────────────────────────────────────────────

// GET /profiles — list all profiles (by in-memory run IDs)
app.MapGet("/profiles", (TornPagesEngine engine) =>
{
    // In Phase 2 this could hit a database; for now profiles = in-memory runs
    return Results.Ok(new { profiles = engine.ListProfiles() });
});

// POST /profiles — create a new profile and optionally start a run
app.MapPost("/profiles", (CreateProfileRequest req, TornPagesEngine engine) =>
{
    var profileId = req.ProfileId ?? Guid.NewGuid().ToString();
    var seed = req.Seed ?? new Random().Next();
    var difficulty = req.Difficulty ?? DifficultyLevel.Normal;
    var run = engine.CreateRun(profileId, seed, difficulty);
    return Results.Ok(new ProfileSummary(
        ProfileId: profileId,
        Name: req.Name ?? profileId,
        HasActiveRun: true,
        ChapterNumber: run.Pages[run.CurrentPageIndex].Left.ChapterNumber));
});

// DELETE /profiles/{id} — delete a profile and its run
app.MapDelete("/profiles/{id}", (string id, TornPagesEngine engine) =>
{
    engine.DeleteRun(id);
    return Results.Ok(new { deleted = id });
});

// ─── Game state ───────────────────────────────────────────────────────────────

// GET /profiles/{id}/state — current page render
app.MapGet("/profiles/{id}/state", (string id, TornPagesEngine engine) =>
{
    try
    {
        var state = engine.GetState(id);
        return Results.Ok(state);
    }
    catch (EngineException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// GET /profiles/{id}/history/{pageIndex} — historical page render
app.MapGet("/profiles/{id}/history/{pageIndex:int}", (string id, int pageIndex, TornPagesEngine engine) =>
{
    try
    {
        var state = engine.GetHistoricalState(id, pageIndex);
        return Results.Ok(state);
    }
    catch (EngineException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// POST /profiles/{id}/action — apply a player action
app.MapPost("/profiles/{id}/action", (string id, PlayerAction action, TornPagesEngine engine) =>
{
    try
    {
        var state = engine.ApplyAction(id, action);
        return Results.Ok(state);
    }
    catch (EngineException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ─── Dev / diagnostics ────────────────────────────────────────────────────────

// GET /health
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow }));

// GET /profiles/{id}/run-json — raw serialized run (for debugging)
app.MapGet("/profiles/{id}/run-json", (string id, TornPagesEngine engine) =>
{
    try
    {
        var json = engine.SerializeRun(id);
        return Results.Content(json, "application/json");
    }
    catch (EngineException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.Run();

// ─── Request types ────────────────────────────────────────────────────────────

public record CreateProfileRequest(
    string? ProfileId = null,
    string? Name = null,
    int? Seed = null,
    DifficultyLevel? Difficulty = null);
