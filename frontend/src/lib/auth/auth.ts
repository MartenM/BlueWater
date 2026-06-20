import { apiClient } from '$lib/api/client';
import { LoginRequest } from '$lib/api/apiClient';
import { session } from './session.svelte';

export async function login(email: string, password: string): Promise<void> {
	const result = await apiClient.login(new LoginRequest({ email, password }));
	session.setTokens({ accessToken: result.accessToken, refreshToken: result.refreshToken });
}

export async function logout(): Promise<void> {
	try {
		await apiClient.logout();
	} finally {
		session.clear();
	}
}
