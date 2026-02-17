let explorerConfig: { basePath: string } | null = null;

export async function loadConfig(): Promise<void> {
  const response = await fetch('./configuration.json');
  explorerConfig = await response.json();
}

export function getExplorerConfig() {
  return explorerConfig;
}

function getApiBase(): string {
  return explorerConfig?.basePath ?? "";
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const base = getApiBase();
  const url = `${base}/api${path}`;
  const response = await fetch(url, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

export async function apiPost<T = void>(path: string, init?: RequestInit): Promise<T> {
  const base = getApiBase();
  const url = `${base}/api${path}`;
  const response = await fetch(url, {
    method: 'POST',
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }

  const text = await response.text();
  return text ? JSON.parse(text) : (undefined as T);
}
