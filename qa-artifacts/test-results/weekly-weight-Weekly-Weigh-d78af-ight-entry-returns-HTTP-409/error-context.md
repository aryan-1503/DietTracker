# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: weekly-weight.spec.ts >> Weekly Weight Tracking Feature — Cycle 2 >> QA-8 (API): Duplicate weight entry returns HTTP 409
- Location: test-cases\weekly-weight.spec.ts:287:7

# Error details

```
Error: expect(received).not.toBeNull()

Received: null
```

# Test source

```ts
  189 | 
  190 |     // Click Enter Later
  191 |     const laterBtn = overlay.locator('button:has-text("Enter Later"), button:has-text("Remind")');
  192 |     await expect(laterBtn).toBeVisible();
  193 |     await laterBtn.click();
  194 |     await expect(overlay).toBeHidden({ timeout: 3000 });
  195 |     await screenshot(page, '07_after_enter_later_dismissed');
  196 | 
  197 |     // Navigate to another route and back — popup should re-appear
  198 |     await page.click('button[aria-label="Profile"], .db-icon-btn:not(.db-icon-btn--danger)');
  199 |     await page.waitForURL(/profile/, { timeout: 5000 }).catch(() => {});
  200 |     await page.goBack();
  201 |     await page.waitForURL(/dashboard/, { timeout: 5000 }).catch(() => {});
  202 |     await page.waitForTimeout(3000);
  203 | 
  204 |     const overlayAfterNav = page.locator('.ww-overlay, [role="dialog"]');
  205 |     const popupReturned = await overlayAfterNav.isVisible({ timeout: 4000 }).catch(() => false);
  206 |     expect(popupReturned, 'Popup should re-appear after Enter Later + navigation').toBeTruthy();
  207 |     await screenshot(page, '08_popup_reappeared_after_enter_later');
  208 |   });
  209 | 
  210 |   // ── QA-9 / AC-7: Invalid weight below min (10 kg) ────────────────────────
  211 |   test('QA-9: Weight < 20 kg disables Save button', async ({ page }) => {
  212 |     await loginViaUI(page);
  213 |     await page.waitForTimeout(3000);
  214 | 
  215 |     const overlay = page.locator('.ww-overlay, [role="dialog"]');
  216 |     const popupVisible = await overlay.isVisible({ timeout: 4000 }).catch(() => false);
  217 | 
  218 |     if (!popupVisible) {
  219 |       console.log('  No popup to test validation — simulating: weight already logged');
  220 |       await screenshot(page, '09_validation_skipped_no_popup');
  221 |       return;
  222 |     }
  223 | 
  224 |     const input = overlay.locator('input[type="number"]');
  225 |     await input.fill('10');
  226 |     await page.waitForTimeout(500);
  227 | 
  228 |     const saveBtn = overlay.locator('button:has-text("Save")');
  229 |     await expect(saveBtn).toBeDisabled();
  230 | 
  231 |     const validationMsg = overlay.locator('.ww-modal__validation');
  232 |     await expect(validationMsg).toBeVisible();
  233 |     await screenshot(page, '09_validation_below_min', true);
  234 |     console.log('  Validation shown for weight 10 kg');
  235 |   });
  236 | 
  237 |   // ── QA-10 / AC-7: Invalid weight above max (400 kg) ──────────────────────
  238 |   test('QA-10: Weight > 300 kg disables Save button', async ({ page }) => {
  239 |     await loginViaUI(page);
  240 |     await page.waitForTimeout(3000);
  241 | 
  242 |     const overlay = page.locator('.ww-overlay, [role="dialog"]');
  243 |     const popupVisible = await overlay.isVisible({ timeout: 4000 }).catch(() => false);
  244 | 
  245 |     if (!popupVisible) {
  246 |       console.log('  No popup to test validation — weight already logged');
  247 |       await screenshot(page, '10_validation_skipped_no_popup');
  248 |       return;
  249 |     }
  250 | 
  251 |     const input = overlay.locator('input[type="number"]');
  252 |     await input.fill('400');
  253 |     await page.waitForTimeout(500);
  254 | 
  255 |     const saveBtn = overlay.locator('button:has-text("Save")');
  256 |     await expect(saveBtn).toBeDisabled();
  257 | 
  258 |     const validationMsg = overlay.locator('.ww-modal__validation');
  259 |     await expect(validationMsg).toBeVisible();
  260 |     await screenshot(page, '10_validation_above_max', true);
  261 |   });
  262 | 
  263 |   // ── QA-11 / AC-7: Non-numeric input ──────────────────────────────────────
  264 |   test('QA-11: Non-numeric input (abc) disables Save button', async ({ page }) => {
  265 |     await loginViaUI(page);
  266 |     await page.waitForTimeout(3000);
  267 | 
  268 |     const overlay = page.locator('.ww-overlay, [role="dialog"]');
  269 |     const popupVisible = await overlay.isVisible({ timeout: 4000 }).catch(() => false);
  270 | 
  271 |     if (!popupVisible) {
  272 |       console.log('  No popup for non-numeric test — weight already logged');
  273 |       await screenshot(page, '11_nonnumeric_skipped');
  274 |       return;
  275 |     }
  276 | 
  277 |     const input = overlay.locator('input[type="number"]');
  278 |     await input.fill('abc');
  279 |     await page.waitForTimeout(500);
  280 | 
  281 |     const saveBtn = overlay.locator('button:has-text("Save")');
  282 |     await expect(saveBtn).toBeDisabled();
  283 |     await screenshot(page, '11_nonnumeric_disabled_save', true);
  284 |   });
  285 | 
  286 |   // ── QA-8 / AC-6: Duplicate entry returns 409 ─────────────────────────────
  287 |   test('QA-8 (API): Duplicate weight entry returns HTTP 409', async ({ request }) => {
  288 |     const token = await loginViaApi(request);
> 289 |     expect(token).not.toBeNull();
      |                       ^ Error: expect(received).not.toBeNull()
  290 | 
  291 |     // Determine current ISO week
  292 |     const now = new Date();
  293 |     const prevWeek = new Date(now);
  294 |     prevWeek.setDate(now.getDate() - 7);
  295 | 
  296 |     // Use a helper to get ISO week of prevWeek
  297 |     const getIsoWeek = (d: Date) => {
  298 |       const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  299 |       const dayNum = date.getUTCDay() || 7;
  300 |       date.setUTCDate(date.getUTCDate() + 4 - dayNum);
  301 |       const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  302 |       return Math.ceil((((date.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  303 |     };
  304 | 
  305 |     const year = prevWeek.getUTCFullYear();
  306 |     const week = getIsoWeek(prevWeek);
  307 |     const payload = { weightKg: 78.5, weekNumber: week, year };
  308 | 
  309 |     // First POST (may succeed or 409 if already exists)
  310 |     const first = await request.post(`${API_URL}/weight-entries`, {
  311 |       data: payload,
  312 |       headers: { Authorization: `Bearer ${token}` },
  313 |       ignoreHTTPSErrors: true,
  314 |     });
  315 |     console.log(`  First POST returned: ${first.status()}`);
  316 | 
  317 |     if (first.status() === 201) {
  318 |       // Second POST must return 409
  319 |       const second = await request.post(`${API_URL}/weight-entries`, {
  320 |         data: payload,
  321 |         headers: { Authorization: `Bearer ${token}` },
  322 |         ignoreHTTPSErrors: true,
  323 |       });
  324 |       expect(second.status()).toBe(409);
  325 |       const body = await second.json();
  326 |       expect(body.message || JSON.stringify(body)).toMatch(/already exists/i);
  327 |       console.log(`  Duplicate POST returned 409 with message: ${body.message}`);
  328 |     } else if (first.status() === 409) {
  329 |       // Entry already existed — 409 on first call also satisfies the test
  330 |       console.log('  Entry already exists; 409 on first call — AC-6 satisfied');
  331 |       expect(first.status()).toBe(409);
  332 |     } else {
  333 |       throw new Error(`Unexpected status from first POST: ${first.status()}`);
  334 |     }
  335 |   });
  336 | 
  337 |   // ── QA-15 / AC-10: Chart empty state when no entries ─────────────────────
  338 |   test('AC-10: Dashboard shows empty-state text when no weight entries', async ({ page, request }) => {
  339 |     // Verify the GET endpoint returns entries array
  340 |     const token = await loginViaApi(request);
  341 |     if (token) {
  342 |       const resp = await request.get(`${API_URL}/weight-entries`, {
  343 |         headers: { Authorization: `Bearer ${token}` },
  344 |         ignoreHTTPSErrors: true,
  345 |       });
  346 |       const entries = await resp.json();
  347 |       console.log(`  Weight entries count: ${Array.isArray(entries) ? entries.length : 'N/A'}`);
  348 | 
  349 |       if (Array.isArray(entries) && entries.length === 0) {
  350 |         await loginViaUI(page);
  351 |         const emptyState = page.locator('text=/No weight entries yet/i');
  352 |         await expect(emptyState).toBeVisible({ timeout: 5000 });
  353 |         await screenshot(page, '12_chart_empty_state');
  354 |       } else {
  355 |         // Entries exist — verify chart renders SVG
  356 |         await loginViaUI(page);
  357 |         const chartSection = page.locator('section:has(h2:text("Weekly Weight"))');
  358 |         await expect(chartSection).toBeVisible();
  359 |         const svg = chartSection.locator('svg');
  360 |         await expect(svg).toBeVisible({ timeout: 5000 });
  361 |         await screenshot(page, '12_chart_with_data');
  362 |         console.log('  Chart SVG is visible with entries');
  363 |       }
  364 |     }
  365 |   });
  366 | 
  367 |   // ── QA-6 / AC-8: Chart week labels are "Week N" ──────────────────────────
  368 |   test('QA-6 + AC-8: Weight chart x-axis labels use "Week N" format', async ({ page, request }) => {
  369 |     const token = await loginViaApi(request);
  370 |     expect(token).not.toBeNull();
  371 | 
  372 |     const resp = await request.get(`${API_URL}/weight-entries`, {
  373 |       headers: { Authorization: `Bearer ${token}` },
  374 |       ignoreHTTPSErrors: true,
  375 |     });
  376 |     expect(resp.ok()).toBeTruthy();
  377 |     const entries = await resp.json();
  378 |     console.log(`  Entries from API: ${JSON.stringify(entries).slice(0, 200)}`);
  379 | 
  380 |     if (Array.isArray(entries) && entries.length > 0) {
  381 |       // Validate weekLabel format
  382 |       for (const e of entries) {
  383 |         expect(e.weekLabel).toMatch(/^Week \d+$/);
  384 |       }
  385 |       console.log(`  All ${entries.length} entries have correct "Week N" labels`);
  386 |     } else {
  387 |       console.log('  No entries to validate labels — empty state tested instead');
  388 |     }
  389 | 
```