const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export interface RenderState {
  message: string;
  pingCount: number;
  pageType: string;
}

export interface PlayerAction {
  actionType: string;
  payload?: Record<string, string>;
}

async function request<T>(path: string, options?: RequestInit): Promise<{ data: T; durationMs: number }> {
  const start = performance.now();
  const res = await fetch(`${API_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  const durationMs = Math.round(performance.now() - start);
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error ?? 'Request failed');
  }
  const data: T = await res.json();
  return { data, durationMs };
}

export const apiClient = {
  getState: () => request<RenderState>('/state'),
  postAction: (action: PlayerAction) =>
    request<RenderState>('/action', {
      method: 'POST',
      body: JSON.stringify(action),
    }),
  getHistory: (pageIndex: number) => request<RenderState>(`/history/${pageIndex}`),
};
