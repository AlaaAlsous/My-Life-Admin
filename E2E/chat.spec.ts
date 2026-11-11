import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';
import { uploadDocument } from './documents.helpers';

test.describe('Chat / AI-assistent', () => {
  test('skickar ett meddelande och får svar', async ({ page }) => {
    await register(page, uniqueUser('chat'));
    await page.goto('/dashboard');

    await page.getByRole('button', { name: 'Open AI chat' }).click();
    await expect(page.getByRole('heading', { name: 'AI assistant' })).toBeVisible();

    // Unikt meddelande så att det inte kolliderar med assistentens svar.
    const message = `Ping ${Date.now()}`;
    await page.locator('#chat-input').fill(message);
    await page.getByRole('button', { name: 'Send' }).click();

    // Användarens meddelande visas i sin egen (högerställda) bubbla ...
    await expect(page.locator('.self-end', { hasText: message })).toBeVisible();
    // ... och assistenten svarar i en egen bubbla (svarets innehåll beror på
    // AI-konfigurationen, så vi assertar bara att ett svar kommer).
    await expect(page.locator('.self-start').first()).toBeVisible();
  });

  test('dokumentchatten sparar historiken', async ({ page }) => {
    await register(page, uniqueUser('chat-doc'));

    const title = `ChatDoc ${Date.now()}`;
    await uploadDocument(page, { title });

    // Öppna dokumentet och dess chatt ("Asking about this document").
    await page.locator('li', { hasText: title }).getByRole('link').click();
    await page.getByRole('button', { name: 'Open AI chat' }).click();

    const message = `Kom ihåg detta ${Date.now()}`;
    await page.locator('#chat-input').fill(message);
    await page.getByRole('button', { name: 'Send' }).click();
    await expect(page.locator('.self-end', { hasText: message })).toBeVisible();

    // Ladda om sidan -> dokumentchatts-historiken ska finnas kvar.
    await page.reload();
    await page.getByRole('button', { name: 'Open AI chat' }).click();
    await expect(page.locator('.self-end', { hasText: message })).toBeVisible();
  });
});
