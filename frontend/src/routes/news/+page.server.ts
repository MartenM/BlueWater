import { API_BASE_URL_INTERNAL } from '$env/static/private';
import { Client } from '$lib/api/apiClient';
import { accessTokenAuthHeader } from '$lib/server/auth';
import type { PageServerLoad } from './$types';

const NEWS_PAGE_SIZE = 5;

export const load: PageServerLoad = async ({ url, fetch, cookies }) => {
	const page = Number(url.searchParams.get('page') ?? '1') || 1;
	const authHeader = accessTokenAuthHeader(cookies);
	const client = new Client(API_BASE_URL_INTERNAL, {
		fetch: (input, init) => fetch(input, { ...init, headers: { ...init?.headers, ...authHeader } })
	});

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
