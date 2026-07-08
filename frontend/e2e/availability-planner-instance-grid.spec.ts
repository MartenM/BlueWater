import { expect, test } from '@playwright/test';

// NOTE: seeded data only gives the admin test user access (membership + OutingPlannerUse) to the
// "Beheerders" instance, which has a single member (admin). There is no seeded instance where
// admin is a member alongside another member of a group with OutingPlannerUse, so the "another
// member's row is read-only" scenario from the plan cannot be exercised here without new seed
// data or a second authenticated user — see the implementation report for this gap.

test('navigate via sidebar into a team grid, own row is editable, week nav reloads data', async ({
	page
}) => {
	await page.goto('/tools/availability-planner');
	await page.waitForLoadState('networkidle');

	await expect(page.getByRole('heading', { name: 'Mijn teams' })).toBeVisible();
	const teamLink = page.getByRole('link').filter({ hasText: 'Beheerders' }).first();
	await expect(teamLink).toBeVisible();
	await teamLink.click();

	await page.waitForURL(/\/tools\/availability-planner\/instance\/[0-9a-f-]{36}$/i);
	await page.waitForLoadState('networkidle');

	// Role-group heading renders.
	await expect(page.locator('h3').first()).toBeVisible();

	const timeline = page.getByTestId('availability-timeline').first();
	await expect(timeline).toBeVisible();
	await expect(timeline).toHaveAttribute('data-editable', 'true');

	const box = await timeline.boundingBox();
	if (!box) throw new Error('timeline not visible');
	const startX = box.x + box.width * 0.3;
	const endX = box.x + box.width * 0.45;
	const y = box.y + box.height / 2;
	await page.mouse.move(startX, y);
	await page.mouse.down();
	await page.mouse.move(endX, y, { steps: 5 });
	await page.mouse.up();
	await expect(timeline.getByTestId('availability-block')).toHaveCount(1);

	// Week navigation triggers a reload without errors.
	const firstDayHeading = page.locator('main h2').first();
	const initialText = await firstDayHeading.textContent();
	await page.getByRole('button', { name: 'Volgende week' }).click();
	await page.waitForLoadState('networkidle');
	await expect(page.locator('main h2').first()).not.toHaveText(initialText ?? '');
});
