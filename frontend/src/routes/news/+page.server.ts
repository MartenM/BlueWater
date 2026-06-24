import { serverApiClient } from '$lib/server/api/client';
import type { PageServerLoad } from './$types';

const NEWS_PAGE_SIZE = 5;

export const load: PageServerLoad = async ({ url, fetch, cookies }) => {
	const page = Number(url.searchParams.get('page') ?? '1') || 1;
	const client = serverApiClient(cookies, fetch);

	try {
		const result = await client.newsGET(page, NEWS_PAGE_SIZE);
		return {
			page,
			items: result.items.map((item) => ({ ...item })),
			totalCount: result.totalCount,
			error: false
		};
	} catch {
		return { page, items: [], totalCount: 0, error: true };
	}
};
