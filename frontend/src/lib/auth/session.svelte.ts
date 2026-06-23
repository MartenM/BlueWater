import { BluePermission } from '$lib/api/apiClient';
import { decodeJwt } from './jwt';

interface AccessTokenClaims {
	sub: string;
	email: string;
	permission?: BluePermission | BluePermission[];
}

export interface SessionUser {
	id: string;
	email: string;
	permissions: BluePermission[];
}

export function claimsToSessionUser(accessToken: string): SessionUser {
	const claims = decodeJwt<AccessTokenClaims>(accessToken);
	const permission = claims.permission ?? [];
	return {
		id: claims.sub,
		email: claims.email,
		permissions: Array.isArray(permission) ? permission : [permission]
	};
}

let user = $state<SessionUser | null>(null);

export const session = {
	get user() {
		return user;
	},
	hasPermission(permission: BluePermission): boolean {
		return user?.permissions.includes(permission) ?? false;
	},
	setUser(next: SessionUser | null) {
		user = next;
	},
	setUserFromAccessToken(accessToken: string) {
		user = claimsToSessionUser(accessToken);
	},
	clear() {
		user = null;
	}
};
