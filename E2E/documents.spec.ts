import { test, expect } from '@playwright/test';
import { uniqueUser, register } from './auth.helpers';
import { uploadDocument } from './documents.helpers';

test.describe('Documents: upload & search', () => {
  test('laddar upp ett dokument och hittar det via sök', async ({ page }) => {
    await register(page, uniqueUser('docs'));
    await page.goto('/documents');

    const title = `Passport ${Date.now()}`;
    await page.locator('#doc-title').fill(title);
    await page.locator('#doc-category').fill('Identity');
    await page.locator('#doc-description').fill('E2E-uppladdning');
    await page.locator('#doc-file').setInputFiles({
      name: 'passport.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('%PDF-1.4 E2E test', 'utf-8'),
    });
    await page.getByRole('button', { name: 'Upload', exact: true }).click();

    // Dokumentet dyker upp i listan.
    await expect(page.getByText(title)).toBeVisible();

    // En sökträff som inte finns -> tom vy.
    await page.locator('#document-search').fill('finns-inte-xyz');
    await expect(page.getByText('No matching documents')).toBeVisible();

    // Sök på rätt titel -> hittas igen.
    await page.locator('#document-search').fill(title);
    await expect(page.getByText(title)).toBeVisible();
  });
});

test.describe('Documents: filter, download & delete', () => {
  test('filtrerar dokument per kategori', async ({ page }) => {
    await register(page, uniqueUser('filter'));

    const identityTitle = `ID ${Date.now()}`;
    await uploadDocument(page, { title: identityTitle, category: 'Identity' });

    await page.reload();
    const insuranceTitle = `Insurance ${Date.now()}`;
    await uploadDocument(page, { title: insuranceTitle, category: 'Insurance' });

    // Filtrera på "Insurance" -> bara försäkringen syns.
    await page.locator('#category-filter').selectOption('Insurance');
    await expect(page.getByText(insuranceTitle)).toBeVisible();
    await expect(page.getByText(identityTitle)).toBeHidden();

    // Tillbaka till alla kategorier -> båda syns igen.
    await page.locator('#category-filter').selectOption('');
    await expect(page.getByText(identityTitle)).toBeVisible();
    await expect(page.getByText(insuranceTitle)).toBeVisible();
  });

  test('laddar ner ett dokument', async ({ page }) => {
    await register(page, uniqueUser('download'));

    const title = `Download ${Date.now()}`;
    await uploadDocument(page, { title, fileName: 'resume.pdf' });

    const item = page.locator('li', { hasText: title });
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      item.getByRole('button', { name: 'Download' }).click(),
    ]);

    expect(download.suggestedFilename()).toBe('resume.pdf');
  });

  test('raderar ett dokument från listan', async ({ page }) => {
    await register(page, uniqueUser('delete'));

    const title = `Delete ${Date.now()}`;
    await uploadDocument(page, { title });

    await page.locator('li', { hasText: title }).getByRole('button', { name: 'Delete' }).click();

    // Bekräfta raderingen i dialogen.
    const confirm = page.getByRole('alertdialog');
    await expect(confirm).toContainText('Delete document?');
    await confirm.getByRole('button', { name: 'Delete', exact: true }).click();

    await expect(page.getByText(title, { exact: true })).toBeHidden();
  });
});