import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';

function expiryInCurrentMonth(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); 
  const lastDayOfMonth = new Date(year, month + 1, 0).getDate();
  const day = Math.min(now.getDate() + 7, lastDayOfMonth);
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

test.describe('Calendar', () => {
  test('visar ett dokuments utgångsdatum i kalendern', async ({ page }) => {
    await register(page, uniqueUser('cal'));
    await page.goto('/documents');

    const title = `Insurance ${Date.now()}`;
    await page.locator('#doc-title').fill(title);
    await page.locator('#doc-expiry').fill(expiryInCurrentMonth());
    await page.locator('#doc-file').setInputFiles({
      name: 'policy.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('%PDF-1.4', 'utf-8'),
    });
    await page.getByRole('button', { name: 'Upload', exact: true }).click();
    await expect(page.getByText(title)).toBeVisible();

    await page.goto('/events');
    // Händelsen skapas automatiskt med titeln "<titel> expires".
    await expect(page.getByRole('button', { name: `${title} expires` })).toBeVisible();
  });
});

test.describe('Settings', () => {
  test('ändrar e-postinställning och sparar', async ({ page }) => {
    await register(page, uniqueUser('settings'));
    await page.goto('/settings');

    const email = page.locator('#setting-email');
    const before = await email.isChecked();
    await page.getByText('Email reminders', { exact: true }).click();
    await page.getByRole('button', { name: 'Save settings' }).click();

    await expect(page.getByText('Your settings have been saved.')).toBeVisible();
    await expect(email).toBeChecked({ checked: !before });
  });

  test('växlar mörkt läge (sparas direkt)', async ({ page }) => {
    await register(page, uniqueUser('dark'));
    await page.goto('/settings');

    const dark = page.locator('#setting-dark-mode');
    const before = await dark.isChecked();
    await page.getByText('Dark mode', { exact: true }).click();

    await expect(dark).toBeChecked({ checked: !before });
  });
});