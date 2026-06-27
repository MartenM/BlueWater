import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch, cookies, locals }) => {
	requirePermission(locals.user, BluePermission.FleetView, resolve('/'));

	const client = serverApiClient(cookies, fetch);
	try {
		const oarSet = await client.oarSetsGET(params.id);
		return { oarSet, error: false };
	} catch {
		return { oarSet: null, error: true };
	}
};
