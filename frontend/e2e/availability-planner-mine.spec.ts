import { expect, test, type Page } from '@playwright/test';

function waitForDaySave(page: Page) {
	return page.waitForResponse(
		(res) => res.url().includes('/api/Availability/my-week/day') && res.request().method() === 'PUT'
	);
}

test('draw, edit and delete a block on the standalone availability page', async ({ page }) => {
	await page.goto('/tools/availability-planner/mine');
	await page.waitForLoadState('networkidle');

	const timeline = page.getByTestId('availability-timeline').first();
	await expect(timeline).toBeVisible();

	const box = await timeline.boundingBox();
	if (!box) throw new Error('timeline not visible');

	// Draw a block roughly in the middle of the timeline.
	const startX = box.x + box.width * 0.3;
	const endX = box.x + box.width * 0.5;
	const y = box.y + box.height / 2;

	const saved1 = waitForDaySave(page);
	await page.mouse.move(startX, y);
	await page.mouse.down();
	await page.mouse.move(endX, y, { steps: 5 });
	await page.mouse.up();
	await expect(timeline.getByTestId('availability-block')).toHaveCount(1);
	await saved1;

	await page.reload();
	await page.waitForLoadState('networkidle');
	const timelineAfterReload = page.getByTestId('availability-timeline').first();
	await expect(timelineAfterReload.getByTestId('availability-block')).toHaveCount(1);

	// Double-click to open the edit popup and change the end time.
	const block = timelineAfterReload.getByTestId('availability-block').first();
	await block.dblclick();
	const popup = page.locator('[data-availability-popup]');
	await expect(popup).toBeVisible();
	const selects = popup.locator('select');
	const originalWidth = (await block.boundingBox())?.width ?? 0;
	const endOptionCount = await selects.nth(1).locator('option').count();
	await selects.nth(1).selectOption({ index: endOptionCount - 1 });
	const saved2 = waitForDaySave(page);
	await popup.getByRole('button', { name: 'Opslaan' }).click();
	await expect(popup).toBeHidden();
	await saved2;
	await expect
		.poll(async () => (await block.boundingBox())?.width ?? 0)
		.toBeGreaterThan(originalWidth);

	await page.reload();
	await page.waitForLoadState('networkidle');
	const timelineAfterEdit = page.getByTestId('availability-timeline').first();
	await expect(timelineAfterEdit.getByTestId('availability-block')).toHaveCount(1);

	// Right-click to delete.
	const saved3 = waitForDaySave(page);
	await timelineAfterEdit.getByTestId('availability-block').first().click({ button: 'right' });
	await expect(timelineAfterEdit.getByTestId('availability-block')).toHaveCount(0);
	await saved3;

	await page.reload();
	await page.waitForLoadState('networkidle');
	const timelineAfterDelete = page.getByTestId('availability-timeline').first();
	await expect(timelineAfterDelete.getByTestId('availability-block')).toHaveCount(0);
});
