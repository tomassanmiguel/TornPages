import type { RenderState, PlayerAction, ProfileSummary } from './types';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error((err as { error?: string }).error ?? 'Request failed');
  }
  return res.json() as Promise<T>;
}

export const api = {
  // Profile management
  listProfiles: () => request<{ profiles: ProfileSummary[] }>('/profiles'),
  createProfile: (profileId: string, name: string, seed?: number) =>
    request<ProfileSummary>('/profiles', {
      method: 'POST',
      body: JSON.stringify({ profileId, name, seed }),
    }),
  deleteProfile: (profileId: string) =>
    request<{ deleted: string }>(`/profiles/${profileId}`, { method: 'DELETE' }),

  // Game state
  getState: (profileId: string) =>
    request<RenderState>(`/profiles/${profileId}/state`),
  getHistory: (profileId: string, pageIndex: number) =>
    request<RenderState>(`/profiles/${profileId}/history/${pageIndex}`),
  postAction: (profileId: string, action: PlayerAction) =>
    request<RenderState>(`/profiles/${profileId}/action`, {
      method: 'POST',
      body: JSON.stringify(action),
    }),

  // Dev
  health: () => request<{ status: string }>('/health'),
};
