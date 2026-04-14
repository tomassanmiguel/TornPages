namespace TornPages.Engine;

// Phase 1 stub types removed — superseded by RenderState.cs, Actions.cs, RunState.cs.
// Keeping only PingLogEntry for the latency endpoint (still used in Program.cs).
public record PingLogEntry(
    int Index,
    DateTimeOffset Timestamp,
    string? Note = null,
    int? DurationMs = null,
    string? TesterName = null);
