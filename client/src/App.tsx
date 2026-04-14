import { useCallback, useEffect, useState } from 'react';
import { apiClient } from './api/client';
import styles from './App.module.css';

const MAX_HISTORY = 10;

interface PingResult {
  timestamp: string;
  durationMs: number;
  ok: boolean;
  error?: string;
}

function latencyColor(ms: number): string {
  if (ms < 150) return '#4caf50';
  if (ms < 400) return '#ff9800';
  return '#f44336';
}

function latencyLabel(ms: number): string {
  if (ms < 150) return 'Good';
  if (ms < 400) return 'Fair';
  return 'Poor';
}

function formatCopyText(results: PingResult[], name: string): string {
  const successful = results.filter((r) => r.ok);
  if (successful.length === 0) return 'No successful pings.';

  const durations = successful.map((r) => r.durationMs);
  const avg = Math.round(durations.reduce((a, b) => a + b, 0) / durations.length);
  const min = Math.min(...durations);
  const max = Math.max(...durations);
  const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

  return [
    `Torn Pages Latency Test — ${new Date().toLocaleString()}`,
    `Tester: ${name || 'Anonymous'}`,
    `Server: ${apiUrl}`,
    `Pings: ${successful.length}`,
    `Avg: ${avg}ms  |  Min: ${min}ms  |  Max: ${max}ms`,
    `Last ${durations.length}: ${durations.join(', ')}ms`,
  ].join('\n');
}

export default function App() {
  const [results, setResults] = useState<PingResult[]>([]);
  const [serverMessage, setServerMessage] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  const [name, setName] = useState(() => localStorage.getItem('tester-name') ?? '');

  const addResult = useCallback((result: PingResult) => {
    setResults((prev) => [result, ...prev].slice(0, MAX_HISTORY));
  }, []);

  // Initial GET /state on load
  useEffect(() => {
    setIsLoading(true);
    apiClient
      .getState()
      .then(({ data, durationMs }) => {
        setServerMessage(data.message);
        addResult({ timestamp: new Date().toLocaleTimeString(), durationMs, ok: true });
      })
      .catch((err: Error) => {
        addResult({ timestamp: new Date().toLocaleTimeString(), durationMs: 0, ok: false, error: err.message });
        setServerMessage('Could not reach server.');
      })
      .finally(() => setIsLoading(false));
  }, [addResult]);

  const ping = useCallback(async () => {
    setIsLoading(true);
    try {
      const payload = name.trim() ? { note: name.trim() } : undefined;
      const { data, durationMs } = await apiClient.postAction({ actionType: 'Ping', payload });
      setServerMessage(data.message);
      addResult({ timestamp: new Date().toLocaleTimeString(), durationMs, ok: true });
      // Fire-and-forget: record the client-measured round-trip time in the server log
      apiClient.postAction({ actionType: 'PingAck', payload: { durationMs: String(durationMs) } }).catch(() => {});
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      addResult({ timestamp: new Date().toLocaleTimeString(), durationMs: 0, ok: false, error: message });
    } finally {
      setIsLoading(false);
    }
  }, [addResult, name]);

  const copyResults = useCallback(async () => {
    await navigator.clipboard.writeText(formatCopyText(results, name));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }, [results, name]);

  const successful = results.filter((r) => r.ok);
  const durations = successful.map((r) => r.durationMs);
  const avg = durations.length ? Math.round(durations.reduce((a, b) => a + b, 0) / durations.length) : null;
  const min = durations.length ? Math.min(...durations) : null;
  const max = durations.length ? Math.max(...durations) : null;
  const latest = durations[0] ?? null;

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Torn Pages</h1>
        <p className={styles.subtitle}>Latency Test</p>
      </header>

      <main className={styles.main}>
        {/* Name input */}
        <div className={styles.nameRow}>
          <label className={styles.nameLabel} htmlFor="tester-name">Your name</label>
          <input
            id="tester-name"
            className={styles.nameInput}
            type="text"
            placeholder="Anonymous"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              localStorage.setItem('tester-name', e.target.value);
            }}
            maxLength={40}
          />
        </div>

        {/* Big latency indicator */}
        <div className={styles.latencyCard}>
          {latest !== null ? (
            <>
              <span className={styles.latencyMs} style={{ color: latencyColor(latest) }}>
                {latest}ms
              </span>
              <span className={styles.latencyLabel} style={{ color: latencyColor(latest) }}>
                {latencyLabel(latest)}
              </span>
            </>
          ) : (
            <span className={styles.latencyMs} style={{ color: '#888' }}>—</span>
          )}
          <p className={styles.serverMessage}>{serverMessage}</p>
        </div>

        {/* Stats row */}
        {durations.length > 0 && (
          <div className={styles.statsRow}>
            <div className={styles.stat}>
              <span className={styles.statLabel}>Avg</span>
              <span className={styles.statValue}>{avg}ms</span>
            </div>
            <div className={styles.stat}>
              <span className={styles.statLabel}>Min</span>
              <span className={styles.statValue} style={{ color: '#4caf50' }}>{min}ms</span>
            </div>
            <div className={styles.stat}>
              <span className={styles.statLabel}>Max</span>
              <span className={styles.statValue} style={{ color: '#f44336' }}>{max}ms</span>
            </div>
            <div className={styles.stat}>
              <span className={styles.statLabel}>Pings</span>
              <span className={styles.statValue}>{successful.length}</span>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className={styles.actions}>
          <button className={styles.pingButton} onClick={ping} disabled={isLoading}>
            {isLoading ? 'Pinging…' : 'Ping'}
          </button>
          <button
            className={styles.copyButton}
            onClick={copyResults}
            disabled={results.length === 0}
          >
            {copied ? 'Copied!' : 'Copy Results'}
          </button>
        </div>

        {/* History */}
        {results.length > 0 && (
          <table className={styles.history}>
            <thead>
              <tr>
                <th>Time</th>
                <th>Latency</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {results.map((r, i) => (
                <tr key={i}>
                  <td>{r.timestamp}</td>
                  <td style={{ color: r.ok ? latencyColor(r.durationMs) : '#f44336' }}>
                    {r.ok ? `${r.durationMs}ms` : '—'}
                  </td>
                  <td style={{ color: r.ok ? '#4caf50' : '#f44336' }}>
                    {r.ok ? 'OK' : r.error ?? 'Error'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </main>
    </div>
  );
}
