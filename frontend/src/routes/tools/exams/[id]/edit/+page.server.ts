import { requirePermission } from '$lib/server/auth';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ params, locals }) => {
	requirePermission(
		locals.user,
		BluePermission.ExamsModify,
		resolve('/tools/exams/[id]', { id: params.id })
	);
};
