import { test, expect } from '@playwright/test';

test.describe('Landing', () => {
  test('visar hero och features', async ({ page }) => {
    await page.goto('/');

    await expect(
      page.getByRole('heading', { name: /All your personal admin/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: 'Everything you need to stay organized' }),
    ).toBeVisible();
  });

  test('öppnar och växlar mellan login- och registerdialogerna', async ({ page }) => {
    await page.goto('/');

    // "Get Started" öppnar login-dialogen.
    await page.getByRole('button', { name: 'Get Started' }).first().click();
    const loginDialog = page.getByRole('dialog', { name: 'Log in' });
    await expect(loginDialog).toBeVisible();

    // Växla till register-dialogen.
    await loginDialog.getByRole('button', { name: 'Create one' }).click();
    const registerDialog = page.getByRole('dialog', { name: 'Create account' });
    await expect(registerDialog).toBeVisible();

    // Växla tillbaka till login-dialogen (skopat till dialogen eftersom
    // "Log in" även finns i sidhuvudet och sidfoten).
    await registerDialog.getByRole('button', { name: 'Log in', exact: true }).click();
    await expect(page.getByRole('dialog', { name: 'Log in' })).toBeVisible();
  });
});
