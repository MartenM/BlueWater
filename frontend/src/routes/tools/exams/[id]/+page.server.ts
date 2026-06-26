import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch, cookies, locals }) => {
	requirePermission(locals.user, BluePermission.ExamsView, resolve('/'));

	const client = serverApiClient(cookies, fetch);
	try {
		const [examType, assigned] = await Promise.all([
			client.typesGET(params.id),
			client.assigned(params.id)
		]);
		return {
			examType: { id: examType.id, name: examType.name, description: examType.description },
			assigned: assigned.map((a) => ({
				id: a.id,
				userId: a.userId,
				userFullname: a.userFullname,
				obtainedAt: a.obtainedAt
			})),
			error: false
		};
	} catch {
		return { examType: null, assigned: [], error: true };
	}
};
