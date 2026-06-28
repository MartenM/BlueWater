import { expect, test } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();
	await expect(page).toHaveURL('/');
}

test('admin can create, configure and delete a signup', async ({ page }) => {
	await login(page);
	const unique = Date.now();
	const title = `E2E Inschrijving ${unique}`;

	// Navigate to admin signup list
	await page.goto('/tools/signup');
	await page.waitForLoadState('networkidle');

	// Open create modal
	await page.getByRole('button', { name: 'Nieuwe inschrijving' }).click();
	await expect(page.getByRole('heading', { name: 'Nieuwe inschrijving' })).toBeVisible();

	// Fill the form
	await page.getByLabel('Titel').fill(title);
	await page.getByLabel('Clusters').selectOption({ label: 'Alle leden' });
	await page.getByRole('button', { name: 'Aanmaken' }).click();

	// Should redirect to the detail page
	await page.waitForURL(/\/tools\/signup\/[0-9a-f-]{36}$/);
	await expect(page.getByRole('heading', { name: title })).toBeVisible();

	// Switch to fields tab and add a field
	await page.getByRole('button', { name: 'Velden' }).click();
	await page.getByRole('button', { name: 'Veld toevoegen' }).click();
	await expect(page.getByRole('heading', { name: 'Veld toevoegen' })).toBeVisible();
	await page.getByRole('dialog').getByLabel('Naam').fill('Dieetwensen');
	await page.getByRole('button', { name: 'Toevoegen', exact: true }).click();

	// Field should appear in the list
	await expect(page.getByText('Dieetwensen')).toBeVisible();

	// Switch to responses tab — initially empty
	await page.getByRole('button', { name: /Aanmeldingen/ }).click();
	await expect(page.getByText('Nog geen aanmeldingen.')).toBeVisible();

	// Go to the member view and check the signup appears
	await page.goto('/signup');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(title)).toBeVisible();

	// Open the member detail page and submit a response
	await page.getByRole('link', { name: title }).click();
	await page.waitForLoadState('networkidle');
	await expect(page.getByRole('heading', { name: title })).toBeVisible();
	await page.getByLabel('Dieetwensen').fill('Geen noten');
	await page.getByRole('button', { name: 'Aanmelden' }).click();

	// After submit the "Mijn aanmelding" section should appear
	await expect(page.getByText('Mijn aanmelding')).toBeVisible();
	await expect(page.getByText('Aangemeld').first()).toBeVisible();

	// Check "My signups" page
	await page.goto('/signup/my');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(title)).toBeVisible();

	// Back to admin detail — response should now show
	await page.goto('/tools/signup');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(title)).toBeVisible();
	await page.getByText(title).click();
	await page.waitForURL(/\/tools\/signup\/[0-9a-f-]{36}$/);
	await page.getByRole('button', { name: /Aanmeldingen/ }).click();
	await expect(page.getByText('Admin der Localhost')).toBeVisible();

	// Delete the signup
	await page.getByRole('button', { name: 'Verwijderen' }).first().click();
	await expect(page.getByText('Weet je zeker dat je deze inschrijving wilt verwijderen?')).toBeVisible();
	await page.getByRole('button', { name: 'Verwijderen' }).last().click();

	// Should navigate back to the list and signup should be gone
	await page.waitForURL('/tools/signup');
	await expect(page.getByText(title)).not.toBeVisible();
});

test('admin can manage signup categories', async ({ page }) => {
	await login(page);
	const unique = Date.now();
	const categoryTitle = `E2E Categorie ${unique}`;

	await page.goto('/tools/signup/categories');
	await page.waitForLoadState('networkidle');

	// Create category
	await page.getByRole('button', { name: 'Nieuwe categorie' }).click();
	await expect(page.getByRole('heading', { name: 'Nieuwe categorie' })).toBeVisible();
	await page.getByRole('dialog').getByLabel('Naam').fill(categoryTitle);
	await page.getByRole('button', { name: 'Aanmaken' }).click();

	// Category appears in the table
	await expect(page.getByText(categoryTitle)).toBeVisible();

	// Edit category
	await page.getByRole('button', { name: 'Bewerken' }).first().click();
	await expect(page.getByRole('heading', { name: 'Categorie bewerken' })).toBeVisible();
	await page.getByRole('dialog').getByLabel('Naam').fill(categoryTitle + ' bewerkt');
	await page.getByRole('button', { name: 'Opslaan' }).click();
	await expect(page.getByText(categoryTitle + ' bewerkt')).toBeVisible();

	// Delete category
	await page.getByRole('button', { name: 'Verwijderen' }).first().click();
	await expect(page.getByText('Weet je zeker dat je deze categorie wilt verwijderen?')).toBeVisible();
	await page.getByRole('button', { name: 'Verwijderen' }).last().click();
	await expect(page.getByText(categoryTitle + ' bewerkt')).not.toBeVisible();
});

test('signup with end date shows as closed after deadline', async ({ page }) => {
	await login(page);
	const unique = Date.now();
	const title = `E2E Gesloten ${unique}`;

	// Create a signup that closes in the past (within 14-day hide window so it still shows on member list)
	const yesterday = new Date();
	yesterday.setDate(yesterday.getDate() - 1);
	const pastDate = yesterday.toISOString().slice(0, 16); // "YYYY-MM-DDTHH:MM"

	await page.goto('/tools/signup');
	await page.waitForLoadState('networkidle');
	await page.getByRole('button', { name: 'Nieuwe inschrijving' }).click();
	await page.getByLabel('Titel').fill(title);
	await page.getByLabel('Clusters').selectOption({ label: 'Alle leden' });
	await page.getByLabel('Sluitdatum').fill(pastDate);
	await page.getByRole('button', { name: 'Aanmaken' }).click();

	await page.waitForURL(/\/tools\/signup\/[0-9a-f-]{36}$/);

	// Member view: should show "Gesloten"
	await page.goto('/signup');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(title)).toBeVisible();
	await expect(page.getByText(/^Gesloten:/).first()).toBeVisible();

	// Clicking through to detail should show closed message, not form
	await page.getByRole('link', { name: title }).click();
	await page.waitForLoadState('networkidle');
	await expect(page.getByText('Deze inschrijving is gesloten.')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Aanmelden' })).not.toBeVisible();

	// Cleanup
	await page.goto('/tools/signup');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(title)).toBeVisible();
	await page.getByText(title).click();
	await page.waitForURL(/\/tools\/signup\/[0-9a-f-]{36}$/);
	await page.getByRole('button', { name: 'Verwijderen' }).click();
	await page.getByRole('button', { name: 'Verwijderen' }).last().click();
	await page.waitForURL('/tools/signup');
});
