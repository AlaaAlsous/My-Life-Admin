import { test, expect } from '@playwright/test';
import { uniqueUser, register, login, logout } from './auth.helpers';

test.describe('Auth: register → login', () => {
  test('registrerar en ny användare och loggar in direkt', async ({ page }) => {
    await register(page, uniqueUser());
    await expect(page.getByRole('button', { name: 'Log out' })).toBeVisible();
  });

  test('kan logga ut och logga in igen med samma konto', async ({ page }) => {
    const user = uniqueUser();
    await register(page, user);
    await logout(page);
    await login(page, user);
    await expect(page).toHaveURL(/\/dashboard$/);
  });

  test('visar felmeddelande för okänd e-post', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: 'Get Started' }).first().click();

    const dialog = page.getByRole('dialog', { name: 'Log in' });
    await dialog.locator('#login-email').fill('okand@example.com');
    await dialog.locator('#login-password').fill('Password123!');
    await dialog.getByRole('button', { name: 'Log in', exact: true }).click();

    await expect(dialog.getByRole('alert')).toContainText('Incorrect email or password');
  });
});

test.describe('Auth: felfall & validering', () => {
  test('visar felmeddelande för fel lösenord', async ({ page }) => {
    const user = uniqueUser();
    await register(page, user);
    await logout(page);

    await page.goto('/');
    await page.getByRole('button', { name: 'Get Started' }).first().click();

    const dialog = page.getByRole('dialog', { name: 'Log in' });
    await dialog.locator('#login-email').fill(user.email);
    await dialog.locator('#login-password').fill('HeltFelLosen1');
    await dialog.getByRole('button', { name: 'Log in', exact: true }).click();

    await expect(dialog.getByRole('alert')).toContainText('Incorrect email or password');
  });

  test('visar felmeddelande när e-posten redan är registrerad', async ({ page }) => {
    const user = uniqueUser();
    await register(page, user);
    await logout(page);

    await page.goto('/');
    await page.getByRole('button', { name: 'Get Started' }).first().click();
    await page.getByRole('button', { name: 'Create one' }).click();

    const dialog = page.getByRole('dialog', { name: 'Create account' });
    await dialog.locator('#register-name').fill('E2E User');
    await dialog.locator('#register-email').fill(user.email);
    await dialog.locator('#register-password').fill(user.password);
    await dialog.getByRole('button', { name: 'Create account', exact: true }).click();

    await expect(dialog.getByRole('alert')).toContainText('This email is already registered');
  });

  test('visar valideringsfel vid tom registrering', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: 'Get Started' }).first().click();
    await page.getByRole('button', { name: 'Create one' }).click();

    const dialog = page.getByRole('dialog', { name: 'Create account' });
    await dialog.getByRole('button', { name: 'Create account', exact: true }).click();

    await expect(dialog.getByText('Name is required.')).toBeVisible();
    await expect(dialog.getByText('Email is required.')).toBeVisible();
    await expect(dialog.getByText('Password is required.')).toBeVisible();
  });

  test('visar valideringsfel för för kort lösenord', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: 'Get Started' }).first().click();
    await page.getByRole('button', { name: 'Create one' }).click();

    const dialog = page.getByRole('dialog', { name: 'Create account' });
    await dialog.locator('#register-name').fill('E2E User');
    await dialog.locator('#register-email').fill(uniqueUser().email);
    await dialog.locator('#register-password').fill('kort');
    await dialog.getByRole('button', { name: 'Create account', exact: true }).click();

    await expect(dialog.getByText('Password must be at least 8 characters.')).toBeVisible();
  });
});