import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, cookies, locals }) => {
	requirePermission(locals.user, BluePermission.FleetView, resolve('/'));

	const client = serverApiClient(cookies, fetch);
	try {
		const items = await client.typesAll2();
		return { items, error: false };
	} catch {
		return { items: [], error: true };
	}
};
