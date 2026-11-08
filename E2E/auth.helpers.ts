import { type Page, expect } from '@playwright/test';

export interface TestUser {
  name: string;
  email: string;
  password: string;
}

export function uniqueUser(prefix = 'e2e'): TestUser {
  const stamp = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return {
    name: 'E2E User',
    email: `${prefix}.${stamp}@example.com`,
    password: 'Password123!',
  };
}

export async function register(page: Page, user: TestUser): Promise<void> {
  await page.goto('/');
  await page.getByRole('button', { name: 'Get Started' }).first().click();
  await page.getByRole('button', { name: 'Create one' }).click();

  const dialog = page.getByRole('dialog', { name: 'Create account' });
  await dialog.locator('#register-name').fill(user.name);
  await dialog.locator('#register-email').fill(user.email);
  await dialog.locator('#register-password').fill(user.password);
  await dialog.getByRole('button', { name: 'Create account', exact: true }).click();

  // Registrering loggar in automatiskt -> dashboard.
  await expect(page).toHaveURL(/\/dashboard$/);
}

export async function login(page: Page, user: TestUser): Promise<void> {
  await page.goto('/');
  await page.getByRole('button', { name: 'Get Started' }).first().click();

  const dialog = page.getByRole('dialog', { name: 'Log in' });
  await dialog.locator('#login-email').fill(user.email);
  await dialog.locator('#login-password').fill(user.password);
  await dialog.getByRole('button', { name: 'Log in', exact: true }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
}

export async function logout(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/$/);
}