import { browser } from '$app/environment';
import { decodeJwt } from './jwt';

export interface AuthTokens {
	accessToken: string;
	refreshToken: string;
}

interface AccessTokenClaims {
	sub: string;
	email: string;
	permission?: string | string[];
}

export interface SessionUser {
	id: string;
	email: string;
	permissions: string[];
}

const STORAGE_KEY = 'bluewater.auth';

function readStoredTokens(): AuthTokens | null {
	if (!browser) return null;
	const raw = localStorage.getItem(STORAGE_KEY);
	return raw ? JSON.parse(raw) : null;
}

let tokens = $state<AuthTokens | null>(readStoredTokens());

const user = $derived.by<SessionUser | null>(() => {
	if (!tokens) return null;
	const claims = decodeJwt<AccessTokenClaims>(tokens.accessToken);
	const permission = claims.permission ?? [];
	return {
		id: claims.sub,
		email: claims.email,
		permissions: Array.isArray(permission) ? permission : [permission]
	};
});

function persist(next: AuthTokens | null) {
	tokens = next;
	if (!browser) return;
	if (next) localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
	else localStorage.removeItem(STORAGE_KEY);
}

if (browser) {
	window.addEventListener('storage', (event) => {
		if (event.key === STORAGE_KEY) {
			tokens = event.newValue ? JSON.parse(event.newValue) : null;
		}
	});
}

export const session = {
	get tokens() {
		return tokens;
	},
	get user() {
		return user;
	},
	setTokens(next: AuthTokens) {
		persist(next);
	},
	clear() {
		persist(null);
	}
};
