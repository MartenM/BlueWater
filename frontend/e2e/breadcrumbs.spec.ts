import { expect, test } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();
	await expect(page).toHaveURL('/');
}

test('home page has no breadcrumbs', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('navigation', { name: 'Breadcrumb' })).toHaveCount(0);
});

test('news breadcrumb links back to the news list', async ({ page }) => {
	await page.goto('/news');
	await page.waitForLoadState('networkidle');

	const firstItem = page.locator('h3').first();
	const title = await firstItem.textContent();
	await firstItem.click();

	const breadcrumb = page.getByRole('navigation', { name: 'Breadcrumb' });
	await expect(breadcrumb.getByRole('link', { name: 'Nieuws' })).toBeVisible();
	await expect(breadcrumb.getByText(title!.trim())).toBeVisible();

	await breadcrumb.getByRole('link', { name: 'Nieuws' }).click();
	await expect(page).toHaveURL('/news');
});

test('user edit breadcrumb shows the section and current page', async ({ page }) => {
	await login(page);

	await page.goto('/tools/users');
	await page.waitForLoadState('networkidle');

	await page.getByText('Admin der Localhost').click();
	await page.waitForLoadState('networkidle');

	const breadcrumb = page.getByRole('navigation', { name: 'Breadcrumb' });
	await expect(breadcrumb.getByRole('link', { name: 'Gebruikers' })).toBeVisible();
	await expect(breadcrumb.getByText('Admin der Localhost')).toBeVisible();

	await page.getByRole('link', { name: 'Bewerken' }).click();
	await page.waitForLoadState('networkidle');

	await expect(breadcrumb.getByRole('link', { name: 'Gebruikers' })).toBeVisible();
	await expect(breadcrumb.getByText('Gebruiker bewerken')).toBeVisible();
});
