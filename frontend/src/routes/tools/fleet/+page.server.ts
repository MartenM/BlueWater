import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, cookies, locals, url }) => {
	requirePermission(locals.user, BluePermission.FleetView, resolve('/'));

	const page = parseInt(url.searchParams.get('page') ?? '1');
	const search = url.searchParams.get('search') ?? undefined;
	const client = serverApiClient(cookies, fetch);
	try {
		const result = await client.fleetGET(page, 25, search);
		return {
			items: result.items ?? [],
			totalCount: result.totalCount,
			page,
			pageSize: result.pageSize,
			search: search ?? '',
			error: false
		};
	} catch {
		return { items: [], totalCount: 0, page: 1, pageSize: 25, search: '', error: true };
	}
};
