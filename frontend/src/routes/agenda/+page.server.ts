import { serverApiClient } from '$lib/server/api/client';
import { currentYearMonth, monthRangeDates, parseYearMonth } from '$lib/agendaMonth';
import type { PageServerLoad } from './$types';

const MONTHS_SHOWN = 2;

export const load: PageServerLoad = async ({ url, fetch, cookies }) => {
	const yearMonth = parseYearMonth(url.searchParams.get('month')) ?? currentYearMonth();
	const client = serverApiClient(cookies, fetch);
	const { start, end } = monthRangeDates(yearMonth, MONTHS_SHOWN);

	try {
		const items = await client.range(start, end);
		return { yearMonth, items: items.map((item) => ({ ...item })), error: false };
	} catch {
		return { yearMonth, items: [], error: true };
	}
};
