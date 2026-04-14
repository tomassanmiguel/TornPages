import { useState, useCallback, useEffect } from 'react';
import { api } from './api/client';
import { Game } from './components/Game';
import { TooltipLayer } from './components/TooltipLayer';
import type { ProfileSummary } from './api/types';
import styles from './App.module.css';

export default function App() {
  const [profiles, setProfiles] = useState<ProfileSummary[]>([]);
  const [activeProfileId, setActiveProfileId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [newName, setNewName] = useState('');
  const [serverError, setServerError] = useState<string | null>(null);

  const loadProfiles = useCallback(async () => {
    try {
      const { profiles: ps } = await api.listProfiles();
      setProfiles(ps);
      setServerError(null);
    } catch (e) {
      setServerError(e instanceof Error ? e.message : 'Cannot connect to server');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => { loadProfiles(); }, [loadProfiles]);

  const handleCreate = useCallback(async () => {
    const name = newName.trim() || `Adventurer ${Date.now().toString().slice(-4)}`;
    const profileId = name.toLowerCase().replace(/\s+/g, '-') + '-' + Math.random().toString(36).slice(2, 6);
    await api.createProfile(profileId, name);
    setActiveProfileId(profileId);
  }, [newName]);

  const handleDelete = useCallback(async (profileId: string) => {
    if (!confirm('Delete this run permanently?')) return;
    await api.deleteProfile(profileId);
    setProfiles(ps => ps.filter(p => p.profileId !== profileId));
  }, []);

  if (activeProfileId) {
    return (
      <>
        <TooltipLayer />
        <Game
          profileId={activeProfileId}
          onExit={() => { setActiveProfileId(null); loadProfiles(); }}
        />
      </>
    );
  }

  return (
    <>
    <TooltipLayer />
    <div className={styles.menuPage}>
      <div className={styles.menuBox}>
        <h1 className={styles.menuTitle}>Torn Pages</h1>
        <p className={styles.menuSubtitle}>A roguelike story machine</p>

        {serverError && (
          <div className={styles.serverError}>
            Server unavailable: {serverError}
          </div>
        )}

        {isLoading ? (
          <p className={styles.loading}>Connecting…</p>
        ) : (
          <>
            {/* Existing profiles */}
            {profiles.length > 0 && (
              <div className={styles.profileList}>
                <h3 className={styles.sectionLabel}>Continue</h3>
                {profiles.map(p => (
                  <div key={p.profileId} className={styles.profileRow}>
                    <button
                      className={styles.profileBtn}
                      onClick={() => setActiveProfileId(p.profileId)}
                    >
                      <span className={styles.profileName}>{p.name}</span>
                      <span className={styles.profileMeta}>
                        Ch.{p.chapterNumber ?? 0} · {p.hasActiveRun ? 'Active' : 'Complete'}
                      </span>
                    </button>
                    <button
                      className={styles.deleteBtn}
                      onClick={() => handleDelete(p.profileId)}
                      title="Delete run"
                    >×</button>
                  </div>
                ))}
              </div>
            )}

            {/* New run */}
            <div className={styles.newRunSection}>
              <h3 className={styles.sectionLabel}>New Run</h3>
              <div className={styles.newRunRow}>
                <input
                  className={styles.nameInput}
                  type="text"
                  placeholder="Your name (optional)"
                  value={newName}
                  onChange={e => setNewName(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && handleCreate()}
                  maxLength={40}
                />
                <button className={styles.startBtn} onClick={handleCreate}>
                  Begin
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
    </>
  );
}
