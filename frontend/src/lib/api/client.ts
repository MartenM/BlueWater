import { PUBLIC_API_BASE_URL } from '$env/static/public';
import { Client, RefreshRequest } from './apiClient';
import { session, type AuthTokens } from '$lib/auth/session.svelte';
import { isExpiringSoon } from '$lib/auth/jwt';

const refreshClient = new Client(PUBLIC_API_BASE_URL, { fetch });

let refreshing: Promise<AuthTokens | null> | null = null;

async function refreshTokens(refreshToken: string): Promise<AuthTokens | null> {
	try {
		const result = await refreshClient.refresh(new RefreshRequest({ refreshToken }));
		const next: AuthTokens = { accessToken: result.accessToken, refreshToken: result.refreshToken };
		session.setTokens(next);
		return next;
	} catch {
		session.clear();
		return null;
	}
}

async function ensureFreshAccessToken(): Promise<string | null> {
	const current = session.tokens;
	if (!current) return null;
	if (!isExpiringSoon(current.accessToken)) return current.accessToken;

	refreshing ??= refreshTokens(current.refreshToken).finally(() => {
		refreshing = null;
	});
	const refreshed = await refreshing;
	return refreshed?.accessToken ?? null;
}

async function authFetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
	const accessToken = await ensureFreshAccessToken();
	const headers = new Headers(init?.headers);
	if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
	return fetch(url, { ...init, headers });
}

export const apiClient = new Client(PUBLIC_API_BASE_URL, { fetch: authFetch });
