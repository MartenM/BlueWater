import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

test('overview page groups participants by role', async ({ page }) => {
	await page.goto('/tools/outing-planner/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Team').selectOption({ index: 1 });
	await page.getByLabel('Datum en tijd').fill(futureDateTimeLocal(3 + (Date.now() % 20)));
	await page.getByLabel('Boottype').selectOption({ label: 'Vier met stuurman' });
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
	await page.waitForLoadState('networkidle');

	// Assign self as rower
	await page.getByLabel('Mijn rol').selectOption({ label: 'Roeier' });
	await page.waitForLoadState('networkidle');

	// Invite another member and set their role to coach
	const search = page.getByPlaceholder('Zoek een lid...');
	await search.fill('a');
	await page.waitForTimeout(400);
	const results = page.locator('ul button', { hasText: /.+/ });
	await expect(results.first()).toBeVisible();
	const count = await results.count();
	let target = results.first();
	for (let i = 0; i < count; i++) {
		const text = await results.nth(i).innerText();
		if (!text.includes('Admin der Localhost')) {
			target = results.nth(i);
			break;
		}
	}
	const invitedName = (await target.innerText()).trim();
	await target.click();
	await page.getByRole('button', { name: 'Uitnodigen' }).click();
	await page.waitForLoadState('networkidle');

	const inviteeRow = page.locator('div', { hasText: invitedName }).last();
	await inviteeRow.getByRole('combobox').selectOption({ label: 'Coach' });
	await page.waitForLoadState('networkidle');

	await page.goto('/tools/outing-planner');
	await page.waitForLoadState('networkidle');
	await page.screenshot({ path: 'e2e/.tmp-overview.png', fullPage: true });
});
