import { expect, test } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();
	await expect(page).toHaveURL('/');
}

function futureDateInputValue(daysAhead: number): string {
	const date = new Date();
	date.setDate(date.getDate() + daysAhead);
	return date.toISOString().slice(0, 10);
}

test('agenda creation shows field-level validation errors from the API', async ({ page }) => {
	await login(page);

	await page.goto('/agenda/new');
	await page.waitForLoadState('networkidle');

	await page.getByLabel('Titel').fill('E2E validation test');
	await page
		.getByLabel('Omschrijving')
		.fill('Een omschrijving voor de e2e test van het formulier.');
	await page.getByLabel('Datum', { exact: true }).fill(futureDateInputValue(2));
	await page.getByLabel('Einddatum').fill(futureDateInputValue(1));
	await page.getByRole('button', { name: 'Plaatsen' }).click();

	await expect(page.getByRole('alert')).toContainText('Controleer de gemarkeerde velden.');
	await expect(page.getByLabel('Einddatum')).toHaveClass(/border-red-400/);
});

test('agenda creation succeeds with valid input', async ({ page }) => {
	await login(page);

	await page.goto('/agenda/new');
	await page.waitForLoadState('networkidle');

	const title = `E2E agendapunt ${Date.now()}`;
	await page.getByLabel('Titel').fill(title);
	await page
		.getByLabel('Omschrijving')
		.fill('Een omschrijving voor de e2e test van het formulier.');
	await page.getByLabel('Datum', { exact: true }).fill(futureDateInputValue(1));
	await page.getByRole('button', { name: 'Plaatsen' }).click();

	await expect(page).toHaveURL(/\/agenda\/[0-9a-f-]+$/);
	await expect(page.getByRole('heading', { name: title })).toBeVisible();
});

test('agenda item can be edited and deleted', async ({ page }) => {
	await login(page);

	await page.goto('/agenda/new');
	await page.waitForLoadState('networkidle');

	const title = `E2E agendapunt ${Date.now()}`;
	await page.getByLabel('Titel').fill(title);
	await page
		.getByLabel('Omschrijving')
		.fill('Een omschrijving voor de e2e test van het formulier.');
	await page.getByLabel('Datum', { exact: true }).fill(futureDateInputValue(1));
	await page.getByRole('button', { name: 'Plaatsen' }).click();
	await expect(page).toHaveURL(/\/agenda\/[0-9a-f-]+$/);

	await page.getByRole('link', { name: 'Bewerken' }).click();
	await expect(page).toHaveURL(/\/agenda\/[0-9a-f-]+\/edit$/);

	const updatedTitle = `${title} (bewerkt)`;
	await page.getByLabel('Titel').fill(updatedTitle);
	await page.getByRole('button', { name: 'Opslaan' }).click();
	await expect(page).toHaveURL(/\/agenda\/[0-9a-f-]+$/);
	await expect(page.getByRole('heading', { name: updatedTitle })).toBeVisible();

	page.once('dialog', (dialog) => dialog.accept());
	await page.getByRole('button', { name: 'Verwijderen' }).click();
	await expect(page).toHaveURL('/agenda');
});
