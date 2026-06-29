import { expect, test as setup } from '@playwright/test';
import path from 'path';

export const authFile = path.join(import.meta.dirname, '.auth/user.json');

setup('authenticate', async ({ page }) => {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();
	await expect(page).toHaveURL('/');
	await page.context().storageState({ path: authFile });
});
