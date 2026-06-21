import { expect, test } from '@playwright/test';

test('home page renders the nav bar', async ({ page }) => {
	await page.goto('/');

	await expect(page.getByAltText('Club logo')).toBeVisible();
	await expect(page.getByRole('link', { name: 'Contact' })).toBeVisible();
});
