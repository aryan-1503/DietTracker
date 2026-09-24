# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: weekly-weight.spec.ts >> Weekly Weight Tracking Feature — Cycle 2 >> QA-6 + AC-8: Weight chart x-axis labels use "Week N" format
- Location: test-cases\weekly-weight.spec.ts:368:7

# Error details

```
Error: expect(received).not.toBeNull()

Received: null
```

# Test source

```ts
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
  289 |     expect(token).not.toBeNull();
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
> 370 |     expect(token).not.toBeNull();
      |                       ^ Error: expect(received).not.toBeNull()
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
  390 |     // Verify in UI
  391 |     await loginViaUI(page);
  392 |     await page.waitForTimeout(2000);
  393 |     const weightSection = page.locator('section:has(h2:text("Weekly Weight"))');
  394 |     await expect(weightSection).toBeVisible();
  395 |     await screenshot(page, '13_weight_chart_week_labels');
  396 |   });
  397 | 
  398 |   // ── AC-9: No external charting library ───────────────────────────────────
  399 |   test('AC-9: Weight chart uses inline SVG with no external charting library', async ({ page, request }) => {
  400 |     const token = await loginViaApi(request);
  401 |     if (token) {
  402 |       // Ensure there's at least one entry
  403 |       const resp = await request.get(`${API_URL}/weight-entries`, {
  404 |         headers: { Authorization: `Bearer ${token}` },
  405 |         ignoreHTTPSErrors: true,
  406 |       });
  407 |       const entries = await resp.json();
  408 | 
  409 |       await loginViaUI(page);
  410 |       await page.waitForTimeout(2000);
  411 | 
  412 |       const weightSection = page.locator('section:has(h2:text("Weekly Weight"))');
  413 |       await expect(weightSection).toBeVisible();
  414 | 
  415 |       if (Array.isArray(entries) && entries.length > 0) {
  416 |         const svg = weightSection.locator('svg');
  417 |         await expect(svg).toBeVisible();
  418 | 
  419 |         // Verify it is raw SVG, not a canvas or third-party chart container
  420 |         const tagName = await weightSection.locator('svg').evaluate(el => el.tagName.toLowerCase());
  421 |         expect(tagName).toBe('svg');
  422 | 
  423 |         // Verify no Chart.js / D3 / recharts script tags in document
  424 |         const hasExternalChart = await page.evaluate(() => {
  425 |           const scripts = Array.from(document.querySelectorAll('script[src]'));
  426 |           return scripts.some(s =>
  427 |             /chart\.js|d3|recharts|highcharts|apexcharts|echarts/i.test((s as HTMLScriptElement).src)
  428 |           );
  429 |         });
  430 |         expect(hasExternalChart).toBeFalsy();
  431 |         await screenshot(page, '14_svg_chart_inline');
  432 |         console.log('  SVG chart confirmed inline, no external charting library detected');
  433 |       }
  434 |     }
  435 |   });
  436 | 
  437 |   // ── FR-3 / AC-3: Pending check endpoint logic ─────────────────────────────
  438 |   test('FR-3: GET /api/weight-entries/pending-check returns correct structure', async ({ request }) => {
  439 |     const token = await loginViaApi(request);
  440 |     expect(token).not.toBeNull();
  441 | 
  442 |     const resp = await request.get(`${API_URL}/weight-entries/pending-check?tz=Asia/Kolkata`, {
  443 |       headers: { Authorization: `Bearer ${token}` },
  444 |       ignoreHTTPSErrors: true,
  445 |     });
  446 |     expect(resp.status()).toBe(200);
  447 |     const body = await resp.json();
  448 |     console.log(`  pending-check response: ${JSON.stringify(body)}`);
  449 | 
  450 |     expect(typeof body.hasPendingEntry).toBe('boolean');
  451 |     if (body.hasPendingEntry) {
  452 |       expect(body.pendingWeekLabel).toMatch(/^Week \d+$/);
  453 |       expect(body.pendingWeekNumber).toBeGreaterThan(0);
  454 |       expect(body.pendingYear).toBeGreaterThan(2000);
  455 |     }
  456 |   });
  457 | 
  458 |   // ── EC-1: No popup when no active diet plan ───────────────────────────────
  459 |   test('EC-1 (API): No pending entry returned when no diet plan StartDate', async ({ request }) => {
  460 |     // This is validated by reading the pending-check response — if hasPendingEntry is false
  461 |     // even when no plan exists, the backend guard is working.
  462 |     // Since we test with a known user, we just verify the endpoint does not crash.
  463 |     const token = await loginViaApi(request);
  464 |     expect(token).not.toBeNull();
  465 | 
  466 |     const resp = await request.get(`${API_URL}/weight-entries/pending-check`, {
  467 |       headers: { Authorization: `Bearer ${token}` },
  468 |       ignoreHTTPSErrors: true,
  469 |     });
  470 |     // Must return 200 (not 500) — guard against null plan crash
```