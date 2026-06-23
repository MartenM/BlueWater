function base64UrlDecode(segment: string): string {
	const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
	const binary = atob(base64);
	const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
	return new TextDecoder().decode(bytes);
}

export function decodeJwt<T = Record<string, unknown>>(token: string): T {
	const payload = token.split('.')[1];
	return JSON.parse(base64UrlDecode(payload));
}

export function getExpiryMs(token: string): number {
	return decodeJwt<{ exp: number }>(token).exp * 1000;
}

export function isExpiringSoon(token: string, skewMs = 30_000): boolean {
	return Date.now() + skewMs >= getExpiryMs(token);
}
