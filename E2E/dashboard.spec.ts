import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';
import { uploadDocument, dateInDays } from './documents.helpers';

test.describe('Dashboard', () => {
  test('visar uppladdade dokument och kommande händelser i översikten', async ({ page }) => {
    await register(page, uniqueUser('dash'));

    const title = `Dash ${Date.now()}`;
    // Ett datum ~10 dagar framåt -> händelsen hamnar garanterat i "Upcoming events".
    await uploadDocument(page, { title, category: 'Insurance', expiry: dateInDays(10) });

    await page.goto('/dashboard');

    // Dokumentet syns bland de senaste dokumenten ...
    const recent = page.locator('section', {
      has: page.getByRole('heading', { name: 'Recent documents' }),
    });
    await expect(recent.getByText(title)).toBeVisible();

    // ... och den automatiskt skapade händelsen bland kommande händelser.
    const upcoming = page.locator('section', {
      has: page.getByRole('heading', { name: 'Upcoming events' }),
    });
    await expect(upcoming.getByText(`${title} expires`)).toBeVisible();
  });

  test('visar tomma tillstånd för en ny användare', async ({ page }) => {
    await register(page, uniqueUser('dash-empty'));
    await page.goto('/dashboard');

    await expect(page.getByText('No documents yet')).toBeVisible();
    await expect(page.getByText('No upcoming events')).toBeVisible();
    await expect(page.getByText('Nothing due')).toBeVisible();
  });
});
