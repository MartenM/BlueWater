import { expect, test, type Page } from '@playwright/test';

// Seeded data (see backend/src/Bluewater.Infra/Seeding/BluewaterContextSeeder.cs):
// admin@example.com is the sole member of the "Beheerders" instance (OutingPlannerUse granted),
// and "Delta" is a free-fleet, in-order boat usable both as an Outing.BoatId and a Material
// Planner reservation target.
const TEAM_NAME = 'Beheerders';
const BOAT_NAME = 'Delta';

function pad(n: number): string {
	return String(n).padStart(2, '0');
}

function toDateTimeLocal(d: Date): string {
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// Far enough in the future to stay clear of any other seeded/test data, with a bit of jitter
// derived from the current time so repeated runs don't reuse the exact same slot as a previous
// run (whose outings/reservations this spec cleans up anyway, but belt and suspenders).
function outingStart(): Date {
	const d = new Date();
	d.setDate(d.getDate() + 60);
	const jitterMinutes = Math.floor(Date.now() / 1000) % 180;
	d.setHours(8, jitterMinutes, 0, 0);
	return d;
}

// FormField wraps label text and the control in the same <label> element, so a <select>'s
// accessible name ends up including every <option>'s text too — getByLabel(..., { exact: true })
// never matches. Scope by the field's label span text instead, then grab its sibling <select>.
function fieldSelect(page: Page, label: string) {
	return page
		.locator('label')
		.filter({ has: page.getByText(label, { exact: true }) })
		.locator('select');
}

async function selectTeam(page: Page, teamName: string) {
	const select = fieldSelect(page, 'Team');
	const options = await select.locator('option').allTextContents();
	const match = options.find((o) => o.includes(teamName));
	if (!match) throw new Error(`Team option containing "${teamName}" not found`);
	await select.selectOption({ label: match });
}

function currentOutingId(page: Page): string {
	const match = page.url().match(/outing-planner\/([0-9a-f-]{36})$/i);
	if (!match) throw new Error(`Not on an outing detail page: ${page.url()}`);
	return match[1];
}

async function deleteCurrentOuting(page: Page) {
	await page.getByRole('button', { name: 'Verwijderen' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Verwijderen' }).click();
	await page.waitForURL(/\/tools\/outing-planner\/instance\/[0-9a-f-]{36}$/i);
}

test('creating a second outing for an already-booked boat flags the conflict', async ({ page }) => {
	const dateTimeValue = toDateTimeLocal(outingStart());
	let outing1Id = '';
	let outing2Id = '';

	try {
		// Create the first outing and book its boat.
		await page.goto('/tools/outing-planner/new');
		await page.waitForLoadState('networkidle');
		await selectTeam(page, TEAM_NAME);
		await page.getByLabel('Datum en tijd').fill(dateTimeValue);
		const firstCheck = page.waitForResponse(
			(res) => res.url().includes('/api/MaterialReservation/conflict') && res.status() === 200
		);
		await fieldSelect(page, 'Boot').selectOption({ label: BOAT_NAME });
		await firstCheck;
		await expect(page.getByText('Boot is beschikbaar')).toBeVisible();

		await page.getByRole('button', { name: 'Aanmaken' }).click();
		await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
		outing1Id = currentOutingId(page);

		await page.getByRole('button', { name: 'Reserveer boot' }).click();
		await expect(page.getByText('Boot gereserveerd ✓')).toBeVisible();

		// Attempt to create a second outing for the same team/date/time/boat — the creation
		// screen should flag the boat as already reserved before the outing is even submitted.
		await page.goto('/tools/outing-planner/new');
		await page.waitForLoadState('networkidle');
		await selectTeam(page, TEAM_NAME);
		await page.getByLabel('Datum en tijd').fill(dateTimeValue);
		const secondCheck = page.waitForResponse(
			(res) => res.url().includes('/api/MaterialReservation/conflict') && res.status() === 200
		);
		await fieldSelect(page, 'Boot').selectOption({ label: BOAT_NAME });
		await secondCheck;
		await expect(page.getByText(/Boot is al gereserveerd/)).toBeVisible();

		// The outing itself can still be created (only the boat booking action is blocked).
		await page.getByRole('button', { name: 'Aanmaken' }).click();
		await page.waitForURL(/\/tools\/outing-planner\/[0-9a-f-]{36}$/i);
		outing2Id = currentOutingId(page);

		// The second outing's boat is not bookable — attempting to book it also fails.
		await page.getByRole('button', { name: 'Reserveer boot' }).click();
		await expect(page.getByText(/already reserved|Reserveren is mislukt/i)).toBeVisible();
	} finally {
		// Cleanup: delete both outings (deleting outing1 also removes its linked reservation).
		if (outing2Id) {
			await page.goto(`/tools/outing-planner/${outing2Id}`);
			await page.waitForLoadState('networkidle');
			await deleteCurrentOuting(page);
		}
		if (outing1Id) {
			await page.goto(`/tools/outing-planner/${outing1Id}`);
			await page.waitForLoadState('networkidle');
			await deleteCurrentOuting(page);
		}
	}
});
