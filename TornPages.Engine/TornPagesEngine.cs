namespace TornPages.Engine;

/// <summary>
/// The single public entry point for all game logic.
/// Phase 1: stub that supports only ping-pong for latency testing.
/// Phase 2+: full game state machine.
/// </summary>
public class TornPagesEngine
{
    private int _pingCount = 0;
    private readonly List<PingLogEntry> _pingLog = [];

    public RenderState GetState()
    {
        return new RenderState(
            Message: "Torn Pages API is running.",
            PingCount: _pingCount,
            PageType: "Idle"
        );
    }

    public RenderState ApplyAction(PlayerAction action)
    {
        if (action.ActionType == "Ping")
        {
            _pingCount++;
            _pingLog.Add(new PingLogEntry(
                Index: _pingCount,
                Timestamp: DateTimeOffset.UtcNow,
                Note: action.Payload?.GetValueOrDefault("note")
            ));
            return new RenderState(
                Message: $"Pong! Server has received {_pingCount} ping(s).",
                PingCount: _pingCount,
                PageType: "Idle"
            );
        }

        throw new EngineException($"Unknown action type: {action.ActionType}");
    }

    public IReadOnlyList<PingLogEntry> GetPingLog() => _pingLog.AsReadOnly();

    public RenderState GetHistoricalState(int pageIndex)
    {
        // Phase 1 stub — no history yet
        return new RenderState(
            Message: $"Historical page {pageIndex} not yet implemented.",
            PingCount: _pingCount,
            PageType: "Historical"
        );
    }
}

public class EngineException(string message) : Exception(message);
