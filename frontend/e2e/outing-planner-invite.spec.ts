import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
