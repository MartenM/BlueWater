import { goto } from '$app/navigation';
import { page } from '$app/state';
import { session } from './session.svelte';

/**
 * Call from a protected page's `$effect` to redirect guests to /login.
 * Since auth state only lives in localStorage, the server-rendered HTML for a
 * protected route briefly shows as logged-out until this runs on the client.
 *
 * Usage: `$effect(requireAuth);`
 */
export function requireAuth(): void {
	if (!session.tokens) {
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- target includes a dynamic redirectTo query param, not a static route literal resolve() can check
		goto(`/login?redirectTo=${encodeURIComponent(page.url.pathname)}`);
	}
}
