import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

test('invite a non-member user to a specific outing', async ({ page }) => {
	await page.goto('/tools/outing-planner/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Team').selectOption({ index: 1 });
	await page.getByLabel('Datum en tijd').fill(futureDateTimeLocal(3 + (Date.now() % 20)));
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
	await page.waitForLoadState('networkidle');

	const search = page.getByPlaceholder('Zoek een lid...');
	await search.fill('a');
	await page.waitForTimeout(400); // MemberPicker debounces its search

	const results = page.locator('ul button', { hasText: /.+/ });
	await expect(results.first()).toBeVisible();

	// Pick a result that isn't the currently logged-in admin, so this is a genuine invite
	// of someone not otherwise related to the outing.
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

	await expect(page.getByText(invitedName, { exact: true })).toBeVisible();
	await expect(page.getByText('(uitgenodigd)')).toBeVisible();
});
