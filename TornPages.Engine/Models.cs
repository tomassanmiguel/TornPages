namespace TornPages.Engine;

/// <summary>
/// The full view model the React frontend renders from.
/// In Phase 1 this is a stub — only the fields needed for latency testing.
/// </summary>
public record RenderState(
    string Message,
    int PingCount,
    string PageType
);

/// <summary>
/// A player action submitted from the UI.
/// In Phase 1 only "Ping" is supported.
/// </summary>
public record PlayerAction(
    string ActionType,
    Dictionary<string, string>? Payload = null
);

/// <summary>
/// A single entry in the server-side ping log.
/// </summary>
public record PingLogEntry(
    int Index,
    DateTimeOffset Timestamp,
    string? Note = null,
    int? DurationMs = null
);
