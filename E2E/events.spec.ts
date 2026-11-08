import { test, expect, type Page, type Locator } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';
import { uploadDocument, expiryLaterThisMonth } from './documents.helpers';

// Öppnar detaljpanelen för dokumentets automatiskt skapade kalenderhändelse.
// Events visas både i kalendern och i "All Events"-listan, så vi söker inom listan
// för att undvika Playwrights stränga-läge-konflikt på dubblerade träffar.
async function openEventDetails(page: Page, title: string): Promise<Locator> {
  await page.goto('/events');
  const allEvents = page.locator('section[aria-labelledby="all-events-heading"]');
  await allEvents.getByRole('button', { name: `${title} expires` }).first().click();
  return page.locator('section[aria-labelledby="event-details-heading"]');
}

test.describe('Events & påminnelser', () => {
  test('visar händelsedetaljer när man väljer en händelse', async ({ page }) => {
    await register(page, uniqueUser('event'));

    const title = `Event ${Date.now()}`;
    await uploadDocument(page, { title, expiry: expiryLaterThisMonth() });

    const details = await openEventDetails(page, title);
    await expect(details.getByRole('heading', { name: `${title} expires` })).toBeVisible();
    await expect(details.getByText('Category')).toBeVisible();
    await expect(details.getByText('Date', { exact: true })).toBeVisible();
  });

  test('skapar, uppdaterar och raderar en påminnelse', async ({ page }) => {
    await register(page, uniqueUser('reminder'));

    const title = `Reminder ${Date.now()}`;
    await uploadDocument(page, { title, expiry: expiryLaterThisMonth() });

    const details = await openEventDetails(page, title);

    // Skapa en påminnelse.
    await details.getByLabel('Reminder').selectOption({ label: '1 week before' });
    await details.getByRole('button', { name: 'Add reminder' }).click();
    await expect(page.getByText('Reminder created successfully.')).toBeVisible();

    // Uppdatera påminnelsen.
    await details.getByLabel('Change reminder').selectOption({ label: '1 month before' });
    await details.getByRole('button', { name: 'Update reminder' }).click();
    await expect(page.getByText('Reminder updated successfully.')).toBeVisible();

    // Radera påminnelsen (med bekräftelsedialog).
    await details.getByRole('button', { name: 'Delete reminder' }).click();
    const confirm = page.getByRole('alertdialog');
    await expect(confirm).toContainText('Delete reminder?');
    await confirm.getByRole('button', { name: 'Delete', exact: true }).click();
    await expect(page.getByText('Reminder deleted successfully.')).toBeVisible();
  });

  test('navigerar mellan månader i kalendern', async ({ page }) => {
    await register(page, uniqueUser('month'));
    await page.goto('/events');

    const calendar = page.locator('section[aria-label="Event calendar"]');
    const now = new Date();
    const nextMonth = new Date(now.getFullYear(), now.getMonth() + 1, 1);
    const nextLabel = nextMonth.toLocaleString('en-US', { month: 'long', year: 'numeric' });

    await calendar.getByRole('button', { name: 'Next month' }).click();
    await expect(calendar.getByRole('heading', { name: nextLabel })).toBeVisible();
  });
});
