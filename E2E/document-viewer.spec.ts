import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';
import { uploadDocument, expiryLaterThisMonth } from './documents.helpers';

test.describe('Document viewer', () => {
  test('öppnar ett dokument och redigerar titel och beskrivning', async ({ page }) => {
    await register(page, uniqueUser('viewer'));

    const title = `Viewer ${Date.now()}`;
    await uploadDocument(page, { title, description: 'Original beskrivning' });

    // Öppna detaljvyn via "View".
    await page.locator('li', { hasText: title }).getByRole('link').click();
    await expect(page).toHaveURL(/\/documents\/\d+$/);
    await expect(page.getByRole('heading', { name: title })).toBeVisible();

    const newTitle = `${title} uppdaterad`;
    await page.getByRole('button', { name: 'Edit' }).click();
    await page.locator('#viewer-title').fill(newTitle);
    await page.locator('#viewer-description').fill('Uppdaterad beskrivning');
    await page.getByRole('button', { name: 'Save', exact: true }).click();

    await expect(page.getByRole('heading', { name: newTitle })).toBeVisible();
    await expect(page.getByText('Uppdaterad beskrivning')).toBeVisible();
  });

  test('uppdaterar titeln så att kalenderhändelsen följer med', async ({ page }) => {
    await register(page, uniqueUser('viewer-cal'));

    const title = `Calendar ${Date.now()}`;
    await uploadDocument(page, { title, expiry: expiryLaterThisMonth() });

    await page.locator('li', { hasText: title }).getByRole('link').click();

    const newTitle = `${title} ny`;
    await page.getByRole('button', { name: 'Edit' }).click();
    await page.locator('#viewer-title').fill(newTitle);
    await page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(page.getByRole('heading', { name: newTitle })).toBeVisible();

    // Kalenderhändelsen ska ha fått det nya namnet "<titel>".
    await page.goto('/events');
    const allEvents = page.locator('section[aria-labelledby="all-events-heading"]');
    await expect(
      allEvents.getByRole('button', { name: new RegExp(newTitle) }).first(),
    ).toBeVisible();
  });

  test('laddar ner filen från detaljvyn', async ({ page }) => {
    await register(page, uniqueUser('viewer-dl'));

    const title = `ViewerDownload ${Date.now()}`;
    await uploadDocument(page, { title, fileName: 'viewer.pdf' });

    await page.locator('li', { hasText: title }).getByRole('link').click();

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('button', { name: 'Download' }).click(),
    ]);
    expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
  });

  test('raderar dokumentet från detaljvyn', async ({ page }) => {
    await register(page, uniqueUser('viewer-del'));

    const title = `ViewerDelete ${Date.now()}`;
    await uploadDocument(page, { title });

    await page.locator('li', { hasText: title }).getByRole('link').click();

    // I detaljvyn kräver radering en bekräftelse i dialogen.
    await page.getByRole('button', { name: 'Delete' }).click();
    const confirm = page.getByRole('alertdialog');
    await expect(confirm).toContainText('Delete document?');
    await confirm.getByRole('button', { name: 'Delete', exact: true }).click();

    await expect(page).toHaveURL(/\/documents$/);
    await expect(page.getByText(title, { exact: true })).toBeHidden();
  });
});
