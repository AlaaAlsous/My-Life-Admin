import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';

function expiryInCurrentMonth(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); 
  const lastDayOfMonth = new Date(year, month + 1, 0).getDate();
  const day = Math.min(now.getDate() + 7, lastDayOfMonth);
  const date = new Date(year, month, day, 12, 0, 0, 0);
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
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
    await expect(
      page
        .locator('section[aria-labelledby="all-events-heading"]')
        .getByRole('button', { name: `${title} expires` }),
    ).toBeVisible();
  });
});

test.describe('Settings', () => {
  test('ändrar e-postinställning (sparas direkt)', async ({ page }) => {
    await register(page, uniqueUser('settings'));
    await page.goto('/settings');

    const email = page.locator('#setting-email');
    const before = await email.isChecked();
    await page.getByText('Email reminders', { exact: true }).click();

    await expect(page.getByText('Your settings have been saved.')).toBeVisible();
    await expect(email).toBeChecked({ checked: !before });
  });

  test('växlar mörkt läge (sparas direkt)', async ({ page }) => {
    await register(page, uniqueUser('dark'));
    await page.goto('/settings');

    const dark = page.locator('#setting-dark-mode');
    const before = await dark.isChecked();
    const htmlDarkBefore = await page.locator('html').evaluate((el) => el.classList.contains('dark'));
    await page.getByText('Dark mode', { exact: true }).click();

    // Temat tillämpas direkt och valet sparas.
    await expect(page.getByText('Your settings have been saved.')).toBeVisible();
    await expect(dark).toBeChecked({ checked: !before });
    const htmlDarkAfter = await page.locator('html').evaluate((el) => el.classList.contains('dark'));
    expect(htmlDarkAfter).toBe(!htmlDarkBefore);
  });

  test('ändringar finns kvar efter omladdning', async ({ page }) => {
    await register(page, uniqueUser('persist'));
    await page.goto('/settings');

    const before = await page.locator('#setting-email').isChecked();
    await page.getByText('Email reminders', { exact: true }).click();
    await expect(page.getByText('Your settings have been saved.')).toBeVisible();

    // Ladda om -> ändringen ska finnas kvar (den sparades direkt).
    await page.reload();
    await expect(page.locator('#setting-email')).toBeChecked({ checked: !before });
  });
});