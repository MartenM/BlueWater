import { PUBLIC_API_BASE_URL } from '$env/static/public';
import { Client, RefreshRequest } from './apiClient';
import { session } from '$lib/auth/session.svelte';

let refreshing: Promise<boolean> | null = null;

function credentialedFetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
	return fetch(url, { ...init, credentials: 'include' });
}

async function refreshSession(): Promise<boolean> {
	try {
		const refreshClient = new Client(PUBLIC_API_BASE_URL, { fetch: credentialedFetch });
		const result = await refreshClient.refresh(new RefreshRequest({ refreshToken: '' }));
		session.setUserFromAccessToken(result.accessToken);
		return true;
	} catch {
		session.clear();
		return false;
	}
}

async function authFetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
	const response = await credentialedFetch(url, init);
	if (response.status !== 401) return response;

	refreshing ??= refreshSession().finally(() => {
		refreshing = null;
	});
	const refreshed = await refreshing;
	if (!refreshed) return response;

	return credentialedFetch(url, init);
}

export const apiClient = new Client(PUBLIC_API_BASE_URL, { fetch: authFetch });
