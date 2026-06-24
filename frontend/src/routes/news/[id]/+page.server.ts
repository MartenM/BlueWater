import { serverApiClient } from '$lib/server/api/client';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch, cookies }) => {
	const client = serverApiClient(cookies, fetch);

	try {
		const post = await client.newsGET2(params.id);
		return { post: { ...post }, error: false };
	} catch {
		return { post: null, error: true };
	}
};
