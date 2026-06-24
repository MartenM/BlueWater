import { expect, test } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
	await page.goto('/login');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('E-mailadres').fill('admin@example.com');
	await page.getByLabel('Wachtwoord').fill('admin');
	await page.getByRole('button', { name: 'Inloggen' }).click();
	await expect(page).toHaveURL('/');
}

test('manage a category, group, instance, member and permission end to end', async ({ page }) => {
	await login(page);
	const unique = Date.now();
	const categoryName = `E2E Categorie ${unique}`;
	const groupName = `E2E Groep ${unique}`;

	await page.goto('/tools/groups/categories/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Naam').fill(categoryName);
	await page.getByLabel('Omschrijving').fill('Categorie voor e2e test');
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: 'Categorie bewerken' })).toBeVisible();

	await page.goto('/tools/groups/groups/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Categorie').selectOption({ label: categoryName });
	await page.getByLabel('Naam').fill(groupName);
	await page.getByLabel('Omschrijving').fill('Groep voor e2e test');
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: groupName })).toBeVisible();

	await page.getByRole('link', { name: 'Nieuwe instantie' }).click();
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: groupName })).toBeVisible();

	await page.getByPlaceholder('Zoek gebruiker om toe te voegen').fill('Admin');
	await page.getByRole('button', { name: 'Zoeken' }).click();
	await page.getByRole('button', { name: 'Toevoegen', exact: true }).click();
	await expect(page.getByText('Admin der Localhost')).toBeVisible();

	await page.getByRole('combobox').selectOption({ label: 'AdminViewGroups' });
	await page.getByRole('button', { name: 'Permissie toevoegen' }).click();
	await expect(page.getByText('AdminViewGroups')).toBeVisible();

	await page.goto('/tools/groups');
	await page.waitForLoadState('networkidle');
	await expect(page.getByText(groupName)).toBeVisible();

	await page.getByRole('button', { name: 'Groep toevoegen' }).click();
	await expect(page.getByRole('heading', { name: 'Nieuwe groep-instantie' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Nieuwe groep', exact: true })).toBeVisible();
	await page.getByRole('button', { name: 'Sluiten' }).click();

	await page.getByRole('button', { name: 'Categorieën' }).click();
	await expect(page.getByText(categoryName)).toBeVisible();
});

test('hints at creating a new instance instead when a group name already exists', async ({
	page
}) => {
	await login(page);
	const unique = Date.now();
	const categoryName = `E2E Categorie ${unique}`;
	const groupName = `E2E Dubbel ${unique}`;

	await page.goto('/tools/groups/categories/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Naam').fill(categoryName);
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: 'Categorie bewerken' })).toBeVisible();

	await page.goto('/tools/groups/groups/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Categorie').selectOption({ label: categoryName });
	await page.getByLabel('Naam').fill(groupName);
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: groupName })).toBeVisible();

	await page.goto('/tools/groups/groups/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Categorie').selectOption({ label: categoryName });
	await page.getByLabel('Naam').fill(groupName);
	await expect(page.getByText('Een groep met deze naam bestaat al')).toBeVisible();
});

test('rejects creating an instance when another group with the same name already has one in that season', async ({
	page
}) => {
	await login(page);
	const unique = Date.now();
	const categoryName = `E2E Categorie ${unique}`;
	const sharedName = `E2E Botsing ${unique}`;

	await page.goto('/tools/groups/categories/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Naam').fill(categoryName);
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: 'Categorie bewerken' })).toBeVisible();

	await page.goto('/tools/groups/groups/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Categorie').selectOption({ label: categoryName });
	await page.getByLabel('Naam').fill(sharedName);
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: sharedName })).toBeVisible();
	await page.getByRole('link', { name: 'Nieuwe instantie' }).click();
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: sharedName })).toBeVisible();

	await page.goto('/tools/groups/groups/new');
	await page.waitForLoadState('networkidle');
	await page.getByLabel('Categorie').selectOption({ label: categoryName });
	await page.getByLabel('Naam').fill(sharedName);
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByRole('heading', { name: sharedName })).toBeVisible();
	await page.getByRole('link', { name: 'Nieuwe instantie' }).click();
	await page.getByRole('button', { name: 'Aanmaken' }).click();
	await expect(page.getByText('Controleer de gemarkeerde velden.')).toBeVisible();
});
