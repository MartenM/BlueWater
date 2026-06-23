import type { Handle } from '@sveltejs/kit';
import { resolveUserFromCookies } from '$lib/server/auth';

export const handle: Handle = async ({ event, resolve }) => {
	event.locals.user = await resolveUserFromCookies(event.cookies, event.fetch);
	return resolve(event);
};
