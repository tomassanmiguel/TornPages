using System.Text.Json;

namespace TornPages.Engine;

/// <summary>
/// The single public entry point for all game logic.
/// Manages per-profile run state in memory; persistence is handled by the API layer.
/// </summary>
public class TornPagesEngine
{
    // Profile ID → RunState (in-memory, API layer handles persistence)
    private readonly Dictionary<string, RunState> _runs = [];

    // ─── Profile / Run lifecycle ───────────────────────────────────────────────

    public RunState? GetRun(string profileId)
        => _runs.TryGetValue(profileId, out var run) ? run : null;

    public IReadOnlyList<ProfileSummary> ListProfiles()
    {
        return _runs.Values.Select(run => new ProfileSummary(
            ProfileId: run.Metadata.ProfileId,
            Name: run.Metadata.ProfileId,
            HasActiveRun: !run.Pages[run.CurrentPageIndex].Left.IsRunOver,
            ChapterNumber: run.Pages[run.CurrentPageIndex].Left.ChapterNumber)).ToList();
    }

    public void LoadRun(RunState run)
        => _runs[run.Metadata.ProfileId] = run;

    public RunState CreateRun(string profileId, int seed, DifficultyLevel difficulty)
    {
        var run = RunFactory.CreateNewRun(profileId, seed, difficulty);
        _runs[profileId] = run;
        return run;
    }

    public void DeleteRun(string profileId)
        => _runs.Remove(profileId);

    // ─── State rendering ───────────────────────────────────────────────────────

    public RenderState GetState(string profileId)
    {
        var run = _runs.GetValueOrDefault(profileId)
            ?? throw new EngineException($"No active run for profile '{profileId}'.");
        return RenderGenerator.Generate(run, run.CurrentPageIndex);
    }

    public RenderState GetHistoricalState(string profileId, int pageIndex)
    {
        var run = _runs.GetValueOrDefault(profileId)
            ?? throw new EngineException($"No active run for profile '{profileId}'.");
        if (pageIndex < 0 || pageIndex >= run.Pages.Count)
            throw new EngineException($"Page index {pageIndex} out of range (0–{run.Pages.Count - 1}).");
        return RenderGenerator.Generate(run, pageIndex);
    }

    // ─── Action dispatch ───────────────────────────────────────────────────────

    public RenderState ApplyAction(string profileId, PlayerAction action)
    {
        var run = _runs.GetValueOrDefault(profileId)
            ?? throw new EngineException($"No active run for profile '{profileId}'.");
        var updated = ActionHandler.Apply(run, action);
        _runs[profileId] = updated;
        return RenderGenerator.Generate(updated, updated.CurrentPageIndex);
    }

    // ─── Serialization helpers (for persistence) ──────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public string SerializeRun(string profileId)
    {
        var run = _runs.GetValueOrDefault(profileId)
            ?? throw new EngineException($"No run for profile '{profileId}'.");
        return JsonSerializer.Serialize(run, _jsonOptions);
    }

    public RunState DeserializeRun(string json)
        => JsonSerializer.Deserialize<RunState>(json, _jsonOptions)
            ?? throw new EngineException("Failed to deserialize run state.");
}

public class EngineException(string message) : Exception(message);
