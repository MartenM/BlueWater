import { serverApiClient } from '$lib/server/api/client';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch, cookies }) => {
	const client = serverApiClient(cookies, fetch);

	try {
		const item = await client.agendaGET2(params.id);
		return { item: { ...item }, error: false };
	} catch {
		return { item: null, error: true };
	}
};
