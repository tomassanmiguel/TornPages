import { useState, useCallback, useEffect } from 'react';
import { api } from '../api/client';
import type { RenderState, PlayerAction } from '../api/types';

export type GameStatus = 'idle' | 'loading' | 'playing' | 'error';

export function useGame(profileId: string | null) {
  const [state, setState] = useState<RenderState | null>(null);
  const [status, setStatus] = useState<GameStatus>('idle');
  const [error, setError] = useState<string | null>(null);
  const [historyIndex, setHistoryIndex] = useState<number | null>(null);

  const loadState = useCallback(async () => {
    if (!profileId) return;
    setStatus('loading');
    setError(null);
    try {
      const s = await api.getState(profileId);
      setState(s);
      setHistoryIndex(null);
      setStatus('playing');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load state');
      setStatus('error');
    }
  }, [profileId]);

  useEffect(() => {
    if (profileId) loadState();
  }, [profileId, loadState]);

  const dispatch = useCallback(async (action: PlayerAction) => {
    if (!profileId) return;
    setStatus('loading');
    setError(null);
    try {
      const s = await api.postAction(profileId, action);
      setState(s);
      setHistoryIndex(null);
      setStatus('playing');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Action failed');
      setStatus('playing'); // Don't lock UI on error
    }
  }, [profileId]);

  const viewHistory = useCallback(async (pageIndex: number) => {
    if (!profileId) return;
    try {
      const s = await api.getHistory(profileId, pageIndex);
      setState(s);
      setHistoryIndex(pageIndex);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load history');
    }
  }, [profileId]);

  const returnToCurrent = useCallback(async () => {
    if (!profileId) return;
    setHistoryIndex(null);
    await loadState();
  }, [profileId, loadState]);

  return {
    state,
    status,
    error,
    historyIndex,
    dispatch,
    viewHistory,
    returnToCurrent,
    isLoading: status === 'loading',
    isViewingHistory: historyIndex !== null,
  };
}
