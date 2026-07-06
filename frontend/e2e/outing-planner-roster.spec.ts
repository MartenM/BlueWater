import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

async function createOuting(page: import('@playwright/test').Page, boatTypeName?: string) {
	await page.goto('/tools/outing-planner/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Team').selectOption({ index: 1 });
	await page.getByLabel('Datum en tijd').fill(futureDateTimeLocal(3 + (Date.now() % 20)));
	if (boatTypeName) {
		await page.getByLabel('Boottype').selectOption({ label: boatTypeName });
	}
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
}
