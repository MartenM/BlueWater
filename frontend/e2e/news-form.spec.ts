import { expect, test } from '@playwright/test';

test('news creation shows field-level validation errors from the API', async ({ page }) => {

	await page.goto('/news/new');
	await page.waitForLoadState('networkidle');

	await page.getByLabel('Titel').fill('ab');
	await page.getByLabel('Korte tekst').fill('ab');
	await page.getByRole('button', { name: 'Plaatsen' }).click();

	await expect(page.getByRole('alert')).toContainText('Controleer de gemarkeerde velden.');

	await expect(page.getByLabel('Titel')).toHaveClass(/border-red-400/);
	await expect(page.getByLabel('Korte tekst')).toHaveClass(/border-red-400/);
	await expect(page.getByText(/karakters/i).first()).toBeVisible();
});

test('news creation succeeds with valid input', async ({ page }) => {

	await page.goto('/news/new');
	await page.waitForLoadState('networkidle');

	const title = `E2E nieuwsbericht ${Date.now()}`;
	await page.getByLabel('Titel').fill(title);
	await page.getByLabel('Korte tekst').fill('Een korte tekst voor de e2e test van het formulier.');
	await page.getByRole('button', { name: 'Plaatsen' }).click();

	await expect(page).toHaveURL(/\/news\/[0-9a-f-]+$/);
	await expect(page.getByRole('heading', { name: title })).toBeVisible();
});
