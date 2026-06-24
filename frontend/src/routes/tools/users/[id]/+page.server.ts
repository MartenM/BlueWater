import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch, cookies, locals }) => {
	requirePermission(locals.user, BluePermission.AdminUsersView, resolve('/'));

	const client = serverApiClient(cookies, fetch);
	try {
		const user = await client.getUser(params.id);
		return {
			user: {
				...user,
				address: { ...user.address },
				emergencyAddress: { ...user.emergencyAddress }
			},
			error: false
		};
	} catch {
		return { user: null, error: true };
	}
};
