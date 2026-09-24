# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: weekly-weight.spec.ts >> Weekly Weight Tracking Feature — Cycle 2 >> REGRESSION: GET /api/weight-entries returns valid response
- Location: test-cases\weekly-weight.spec.ts:578:7

# Error details

```
Error: expect(received).not.toBeNull()

Received: null
```

# Test source

```ts
  480 |     const now = new Date();
  481 |     const testDate = new Date(now);
  482 |     testDate.setDate(now.getDate() - 14); // 2 weeks back to avoid conflict
  483 | 
  484 |     const getIsoWeek = (d: Date) => {
  485 |       const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  486 |       const dayNum = date.getUTCDay() || 7;
  487 |       date.setUTCDate(date.getUTCDate() + 4 - dayNum);
  488 |       const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  489 |       return Math.ceil((((date.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  490 |     };
  491 | 
  492 |     const year = testDate.getUTCFullYear();
  493 |     const week = getIsoWeek(testDate);
  494 |     const payload = { weightKg: 72.3, weekNumber: week, year };
  495 | 
  496 |     const resp = await request.post(`${API_URL}/weight-entries`, {
  497 |       data: payload,
  498 |       headers: { Authorization: `Bearer ${token}` },
  499 |       ignoreHTTPSErrors: true,
  500 |     });
  501 | 
  502 |     if (resp.status() === 201) {
  503 |       const body = await resp.json();
  504 |       expect(body.weightKg).toBe(72.3);
  505 |       console.log(`  Decimal weight 72.3 saved and returned as: ${body.weightKg}`);
  506 |     } else if (resp.status() === 409) {
  507 |       // Already logged — acceptable
  508 |       console.log('  Entry already exists for that week — decimal test already passed earlier');
  509 |     } else {
  510 |       const body = await resp.text();
  511 |       throw new Error(`Unexpected status ${resp.status()}: ${body}`);
  512 |     }
  513 |   });
  514 | 
  515 |   // ── FR-13 / AC-6: 409 on duplicate, validation on API level ──────────────
  516 |   test('FR-13 (API): Weight 0 or negative is rejected by API', async ({ request }) => {
  517 |     const token = await loginViaApi(request);
  518 |     expect(token).not.toBeNull();
  519 | 
  520 |     const now = new Date();
  521 |     const getIsoWeek = (d: Date) => {
  522 |       const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  523 |       const dayNum = date.getUTCDay() || 7;
  524 |       date.setUTCDate(date.getUTCDate() + 4 - dayNum);
  525 |       const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  526 |       return Math.ceil((((date.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  527 |     };
  528 |     const year = now.getUTCFullYear();
  529 |     const week = getIsoWeek(now);
  530 | 
  531 |     // Try weight = 0
  532 |     const resp = await request.post(`${API_URL}/weight-entries`, {
  533 |       data: { weightKg: 0, weekNumber: week, year },
  534 |       headers: { Authorization: `Bearer ${token}` },
  535 |       ignoreHTTPSErrors: true,
  536 |     });
  537 |     expect([400, 422]).toContain(resp.status());
  538 |     console.log(`  weight=0 returned ${resp.status()} (validation rejection confirmed)`);
  539 |   });
  540 | 
  541 |   // ── Regression: Dashboard still loads correctly ───────────────────────────
  542 |   test('REGRESSION: Dashboard core sections still render correctly', async ({ page }) => {
  543 |     await loginViaUI(page);
  544 |     await page.waitForTimeout(2000);
  545 | 
  546 |     // Key sections that existed before the feature
  547 |     const todaysProgress = page.locator('section:has(.db-today, .db-card--empty)');
  548 |     await expect(todaysProgress).toBeVisible({ timeout: 5000 });
  549 | 
  550 |     const keyStats = page.locator('section:has(h2:text("Key Statistics"))');
  551 |     await expect(keyStats).toBeVisible();
  552 | 
  553 |     const adherenceChart = page.locator('section:has(h2:text("Reported Plan Following"))');
  554 |     await expect(adherenceChart).toBeVisible();
  555 | 
  556 |     const navCards = page.locator('.db-nav-grid');
  557 |     await expect(navCards).toBeVisible();
  558 | 
  559 |     await screenshot(page, '15_regression_dashboard_core_sections');
  560 |     console.log('  All pre-existing dashboard sections still visible');
  561 |   });
  562 | 
  563 |   // ── Regression: Auth flow still works ─────────────────────────────────────
  564 |   test('REGRESSION: Login and logout work correctly', async ({ page }) => {
  565 |     await loginViaUI(page);
  566 |     await expect(page).toHaveURL(/dashboard/);
  567 | 
  568 |     // Logout
  569 |     const logoutBtn = page.locator('button[aria-label="Sign out"]');
  570 |     await expect(logoutBtn).toBeVisible();
  571 |     await logoutBtn.click();
  572 |     await page.waitForURL(/login|auth/, { timeout: 5000 });
  573 |     await screenshot(page, '16_regression_logout');
  574 |     console.log('  Login/logout regression passed');
  575 |   });
  576 | 
  577 |   // ── Regression: GET /api/weight-entries returns valid array ───────────────
  578 |   test('REGRESSION: GET /api/weight-entries returns valid response', async ({ request }) => {
  579 |     const token = await loginViaApi(request);
> 580 |     expect(token).not.toBeNull();
      |                       ^ Error: expect(received).not.toBeNull()
  581 | 
  582 |     const resp = await request.get(`${API_URL}/weight-entries`, {
  583 |       headers: { Authorization: `Bearer ${token}` },
  584 |       ignoreHTTPSErrors: true,
  585 |     });
  586 |     expect(resp.status()).toBe(200);
  587 |     const body = await resp.json();
  588 |     expect(Array.isArray(body)).toBeTruthy();
  589 |     console.log(`  GET /api/weight-entries returned ${body.length} entries`);
  590 |   });
  591 | });
  592 | 
```