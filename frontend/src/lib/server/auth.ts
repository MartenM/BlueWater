import type { Cookies } from '@sveltejs/kit';
import { redirect } from '@sveltejs/kit';
import { env } from '$env/dynamic/private';
import { BluePermission } from '$lib/api/apiClient';
import { isExpiringSoon } from '$lib/auth/jwt';
import { claimsToSessionUser, type SessionUser } from '$lib/auth/session.svelte';

const ACCESS_TOKEN_COOKIE = 'bw_access_token';
const REFRESH_TOKEN_COOKIE = 'bw_refresh_token';

function parseSetCookie(
	setCookie: string
): { name: string; value: string; options: Record<string, unknown> } | null {
	const parts = setCookie.split(';').map((part) => part.trim());
	const [nameValue, ...attributes] = parts;
	const eqIndex = nameValue.indexOf('=');
	if (eqIndex === -1) return null;
	const name = nameValue.slice(0, eqIndex);
	const value = nameValue.slice(eqIndex + 1);

	const options: Record<string, unknown> = { path: '/' };
	for (const attribute of attributes) {
		const [rawKey, rawVal] = attribute.split('=');
		const key = rawKey.toLowerCase();
		if (key === 'path') options.path = rawVal;
		else if (key === 'domain') options.domain = rawVal;
		else if (key === 'max-age') options.maxAge = Number(rawVal);
		else if (key === 'expires') options.expires = new Date(rawVal);
		else if (key === 'samesite') options.sameSite = rawVal.toLowerCase();
		else if (key === 'secure') options.secure = true;
		else if (key === 'httponly') options.httpOnly = true;
	}
	return { name, value, options };
}

async function refreshFromCookie(
	cookies: Cookies,
	fetch: typeof globalThis.fetch
): Promise<SessionUser | null> {
	const refreshToken = cookies.get(REFRESH_TOKEN_COOKIE);
	if (!refreshToken) return null;

	const response = await fetch(`${env.API_BASE_URL_INTERNAL}/api/Auth/refresh`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
			Cookie: `${REFRESH_TOKEN_COOKIE}=${refreshToken}`
		},
		body: JSON.stringify({ refreshToken: '' })
	});
	if (!response.ok) return null;

	for (const setCookie of response.headers.getSetCookie()) {
		const parsed = parseSetCookie(setCookie);
		if (!parsed) continue;
		// eslint-disable-next-line @typescript-eslint/no-explicit-any -- cookie attributes are assembled generically above
		cookies.set(parsed.name, parsed.value, parsed.options as any);
	}

	const body = (await response.json()) as { accessToken: string };
	return claimsToSessionUser(body.accessToken);
}

export async function resolveUserFromCookies(
	cookies: Cookies,
	fetch: typeof globalThis.fetch
): Promise<SessionUser | null> {
	const accessToken = cookies.get(ACCESS_TOKEN_COOKIE);
	if (accessToken && !isExpiringSoon(accessToken, 0)) {
		return claimsToSessionUser(accessToken);
	}

	try {
		return await refreshFromCookie(cookies, fetch);
	} catch {
		return null;
	}
}

export function accessTokenAuthHeader(cookies: Cookies): Record<string, string> {
	const token = cookies.get(ACCESS_TOKEN_COOKIE);
	return token ? { Authorization: `Bearer ${token}` } : {};
}

export function requireUser(user: SessionUser | null, pathname: string): SessionUser {
	if (!user) {
		redirect(303, `/login?redirectTo=${encodeURIComponent(pathname)}`);
	}
	return user;
}

export function requirePermission(
	user: SessionUser | null,
	permission: BluePermission,
	redirectTo: string
): SessionUser {
	if (!user?.permissions.includes(permission)) {
		redirect(303, redirectTo);
	}
	return user;
}
