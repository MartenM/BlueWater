import { expect, test, type Page } from '@playwright/test';

async function goToAdminUserDetail(page: Page) {
	await page.goto('/tools/users');
	await page.waitForLoadState('networkidle');
	await page.getByPlaceholder('Zoek op naam, gebruikersnaam of e-mailadres').fill('admin');
	await page.keyboard.press('Enter');
	await page.waitForLoadState('networkidle');
	await page.getByText('Admin der Localhost').click();
	await page.waitForLoadState('networkidle');
}

async function generatePng(page: Page, width: number, height: number): Promise<Buffer> {
	const dataUrl = await page.evaluate(
		({ width, height }) => {
			const canvas = document.createElement('canvas');
			canvas.width = width;
			canvas.height = height;
			canvas.getContext('2d')!.fillRect(0, 0, width, height);
			return canvas.toDataURL('image/png');
		},
		{ width, height }
	);
	return Buffer.from(dataUrl.split(',')[1], 'base64');
}

test("admin uploads a profile picture for a user, and it then shows on that user's own profile", async ({
	page
}) => {
	await goToAdminUserDetail(page);

	const picture = await generatePng(page, 75, 100);
	await page
		.locator('input[type="file"]')
		.setInputFiles({ name: 'avatar.png', mimeType: 'image/png', buffer: picture });
	await page.getByRole('button', { name: 'Uploaden' }).click();

	await expect(page.getByAltText('Profielfoto')).toBeVisible();

	await page.goto('/profile');
	await page.waitForLoadState('networkidle');
	await expect(page.getByAltText('Profielfoto')).toBeVisible();
});

test('shows a graceful validation error when the uploaded picture has the wrong dimensions', async ({
	page
}) => {
	await goToAdminUserDetail(page);

	const wrongSize = await generatePng(page, 50, 50);
	await page
		.locator('input[type="file"]')
		.setInputFiles({ name: 'wrong-size.png', mimeType: 'image/png', buffer: wrongSize });
	await page.getByRole('button', { name: 'Uploaden' }).click();

	await expect(page.getByRole('alert')).toBeVisible();
});
