import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

test('create an outing and see it in the personal overview and team upcoming view', async ({
	page
}) => {
	const unique = Date.now();
	const description = `E2E Outing ${unique}`;

	await page.goto('/tools/outing-planner/new');
	await page.waitForLoadState('networkidle');

	const teamName = await page.getByLabel('Team').locator('option').nth(1).textContent();
	await page.getByLabel('Team').selectOption({ index: 1 });
	await page.getByLabel('Datum en tijd').fill(futureDateTimeLocal(4 + (unique % 20)));
	await page.getByLabel('Omschrijving', { exact: true }).fill(description);
	await page.getByRole('button', { name: 'Aanmaken' }).click();

	await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
	await expect(page.getByText(description)).toBeVisible();
	expect(teamName).toBeTruthy();
	const outingId = page.url().split('/').pop()!;

	// Personal overview: the outing should be listed (as a link to its detail page) under its team's section.
	await page.goto('/tools/outing-planner');
	await page.waitForLoadState('networkidle');
	const teamSection = page.locator('div', { has: page.getByRole('heading', { name: teamName! }) });
	await expect(teamSection.locator(`a[href*="${outingId}"]`)).toBeVisible();

	// Team instance page: the outing should show under the "Aankomend" tab.
	await teamSection.getByRole('link', { name: 'Alle outings' }).click();
	await page.waitForLoadState('networkidle');
	await expect(page.getByRole('button', { name: 'Aankomend' })).toHaveClass(/border-primary/);
	await expect(page.locator(`a[href*="${outingId}"]`)).toBeVisible();
});
