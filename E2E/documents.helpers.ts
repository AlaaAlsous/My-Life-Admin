import { type Page, expect } from '@playwright/test';

// Ett datum senare under innevarande månad (klampat till månadens sista dag),
// så att den automatiskt skapade kalenderhändelsen alltid syns i kalenderns
// standardvy och i dashboardens "Upcoming events" – och aldrig ligger i det
// förflutna.
export function expiryLaterThisMonth(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); // 0-baserad
  const lastDayOfMonth = new Date(year, month + 1, 0).getDate();
  const day = Math.min(now.getDate() + 7, lastDayOfMonth);
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

/** Ett datum `days` dagar framåt (YYYY-MM-DD). Kan passera månadsskiftet. */
export function dateInDays(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export interface UploadOptions {
  title: string;
  category?: string;
  description?: string;
  /** YYYY-MM-DD */
  expiry?: string;
  fileName?: string;
}

/**
 * Laddar upp ett dokument via formuläret på /documents och väntar tills det
 * dyker upp i listan. Förutsätter att användaren redan är inloggad.
 *
 * Obs: efter uppladdning hamnar formuläret i "granska"-läge. Anropa
 * `page.reload()` om du behöver ladda upp ytterligare ett dokument i samma test.
 */
export async function uploadDocument(page: Page, options: UploadOptions): Promise<void> {
  await page.goto('/documents');

  await page.locator('#doc-title').fill(options.title);
  if (options.category) {
    await page.locator('#doc-category').fill(options.category);
  }
  if (options.description) {
    await page.locator('#doc-description').fill(options.description);
  }
  if (options.expiry) {
    await page.locator('#doc-expiry').fill(options.expiry);
  }
  await page.locator('#doc-file').setInputFiles({
    name: options.fileName ?? 'document.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.4 E2E test', 'utf-8'),
  });
  await page.getByRole('button', { name: 'Upload', exact: true }).click();

  // Dokumentet ska dyka upp i listan.
  await expect(page.getByText(options.title)).toBeVisible();
}
