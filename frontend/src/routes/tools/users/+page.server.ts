import { requirePermission } from '$lib/server/auth';
import { serverApiClient } from '$lib/server/api/client';
import { BluePermission } from '$lib/api/apiClient';
import { resolve } from '$app/paths';
import type { PageServerLoad } from './$types';

const USERS_PAGE_SIZE = 20;

export const load: PageServerLoad = async ({ url, fetch, cookies, locals }) => {
	requirePermission(locals.user, BluePermission.AdminUsersView, resolve('/'));

	const page = Number(url.searchParams.get('page') ?? '1') || 1;
	const search = url.searchParams.get('search') ?? '';
	const client = serverApiClient(cookies, fetch);

	try {
		const result = await client.listUsers(page, USERS_PAGE_SIZE, search || undefined);
		return {
			page,
			search,
			items: result.items.map((item) => ({
				id: item.id,
				fullname: item.fullname,
				userName: item.userName,
				email: item.email
			})),
			totalCount: result.totalCount,
			error: false
		};
	} catch {
		return { page, search, items: [], totalCount: 0, error: true };
	}
};
