import { requirePermission } from '$lib/server/auth';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ locals }) => {
	requirePermission(locals.user, BluePermission.AgendaModify, resolve('/agenda'));
};
