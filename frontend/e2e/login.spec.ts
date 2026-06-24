import { expect, test } from '@playwright/test';

test('login works and shows the email in the auth nudge', async ({ page }) => {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');

	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();

	await expect(page).toHaveURL('/');
	await expect(page.getByRole('button', { name: 'admin@example.com' })).toBeVisible();
});
