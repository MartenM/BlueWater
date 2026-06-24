import type { Cookies } from '@sveltejs/kit';
import { API_BASE_URL_INTERNAL } from '$env/static/private';
import { Client } from '$lib/api/apiClient';
import { accessTokenAuthHeader } from '$lib/server/auth';

export function serverApiClient(cookies: Cookies, fetch: typeof globalThis.fetch): Client {
	const authHeader = accessTokenAuthHeader(cookies);
	return new Client(API_BASE_URL_INTERNAL, {
		fetch: (input, init) => fetch(input, { ...init, headers: { ...init?.headers, ...authHeader } })
	});
}
