import { requirePermission } from '$lib/server/auth';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
	requirePermission(locals.user, BluePermission.AdminModifyMailings, resolve('/'));
};
