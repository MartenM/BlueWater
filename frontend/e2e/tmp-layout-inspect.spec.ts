import { expect, test } from '@playwright/test';

function futureDateTimeLocal(hoursFromNow: number): string {
	const d = new Date(Date.now() + hoursFromNow * 60 * 60000);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

test('inspect roster layout', async ({ page }) => {
	await page.goto('/tools/outing-planner/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Team').selectOption({ index: 1 });
	await page.getByLabel('Datum en tijd').fill(futureDateTimeLocal(3 + (Date.now() % 20)));
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
	await page.waitForLoadState('networkidle');

	// Screenshot with empty roster (no participants yet, add-row visible per role)
	await page.screenshot({ path: 'e2e/tmp-empty.png', fullPage: true });

	// Assign self as Rower via "Mijn rol" to get one participant card, then screenshot
	await page.getByLabel('Mijn rol').selectOption({ label: 'Roeier' });
	await page.waitForLoadState('networkidle');
	await page.screenshot({ path: 'e2e/tmp-one-participant.png', fullPage: true });

	// Try to add another member via MemberPicker in the Roeier row without pressing Toevoegen yet
	const roeierHeading = page.getByRole('heading', { name: 'Roeier' });
	const roeierBox = roeierHeading.locator('xpath=ancestor::div[1]');
	await roeierBox.getByPlaceholder('Lid zoeken...').click();
	await roeierBox.getByPlaceholder('Lid zoeken...').fill('a');
	await page.waitForTimeout(500);
	await page.screenshot({ path: 'e2e/tmp-picker-open.png', fullPage: true });
});
