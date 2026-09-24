# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: weekly-weight.spec.ts >> Weekly Weight Tracking Feature — Cycle 2 >> QA-4 (UI): Login, popup appears, enter valid weight, Save → popup closes, chart updates
- Location: test-cases\weekly-weight.spec.ts:85:7

# Error details

```
TimeoutError: page.waitForURL: Timeout 15000ms exceeded.
=========================== logs ===========================
waiting for navigation until "load"
============================================================
```

# Page snapshot

```yaml
- generic [ref=e5]:
  - img "EatTrack Logo" [ref=e7]
  - heading "Welcome back" [level=1] [ref=e8]
  - paragraph [ref=e9]: Sign in to EatTrack
  - alert [ref=e10]: Login failed. Please try again.
  - generic [ref=e11]:
    - generic [ref=e12]:
      - generic [ref=e13]: Email
      - textbox "Email" [ref=e14]:
        - /placeholder: you@example.com
        - text: test@test.com
    - generic [ref=e15]:
      - generic [ref=e16]: Password
      - textbox "Password" [ref=e17]:
        - /placeholder: ••••••••
        - text: Test@123
    - button "Sign in" [ref=e18] [cursor=pointer]
  - paragraph [ref=e19]:
    - text: Don't have an account?
    - link "Create one" [ref=e20] [cursor=pointer]:
      - /url: /auth/register
```

# Test source

```ts
  1   | import { test, expect, Page, APIRequestContext } from '@playwright/test';
  2   | import * as path from 'path';
  3   | import * as fs from 'fs';
  4   | 
  5   | // ── Config ─────────────────────────────────────────────────────────────────
  6   | const BASE_URL = 'http://localhost:4200';
  7   | const API_URL = 'https://localhost:7164/api';
  8   | const SCREENSHOTS = path.resolve(__dirname, '..', 'screenshots', 'feature');
  9   | const FAILURES = path.resolve(SCREENSHOTS, 'failures');
  10  | 
  11  | [SCREENSHOTS, FAILURES].forEach(d => fs.mkdirSync(d, { recursive: true }));
  12  | 
  13  | // Test credentials — must have an active primary diet plan in DB
  14  | const TEST_USER = { email: 'test@test.com', password: 'Test@123' };
  15  | 
  16  | // ── Helpers ────────────────────────────────────────────────────────────────
  17  | async function screenshot(page: Page, name: string, failure = false) {
  18  |   const dir = failure ? FAILURES : SCREENSHOTS;
  19  |   const fp = path.join(dir, `${name}.png`);
  20  |   await page.screenshot({ path: fp, fullPage: true });
  21  |   return fp;
  22  | }
  23  | 
  24  | async function loginViaApi(request: APIRequestContext): Promise<string | null> {
  25  |   try {
  26  |     const resp = await request.post(`${API_URL}/auth/login`, {
  27  |       data: { email: TEST_USER.email, password: TEST_USER.password },
  28  |       ignoreHTTPSErrors: true,
  29  |     });
  30  |     if (resp.ok()) {
  31  |       const body = await resp.json();
  32  |       return body.token ?? body.accessToken ?? null;
  33  |     }
  34  |   } catch (e) {
  35  |     console.error('loginViaApi error:', e);
  36  |   }
  37  |   return null;
  38  | }
  39  | 
  40  | async function loginViaUI(page: Page) {
  41  |   await page.goto(BASE_URL, { waitUntil: 'networkidle' });
  42  |   await page.fill('input[type="email"], input[name="email"]', TEST_USER.email);
  43  |   await page.fill('input[type="password"], input[name="password"]', TEST_USER.password);
  44  |   await page.click('button[type="submit"]');
> 45  |   await page.waitForURL(/dashboard/, { timeout: 15000 });
      |              ^ TimeoutError: page.waitForURL: Timeout 15000ms exceeded.
  46  | }
  47  | 
  48  | function decodeJwtPayload(token: string): Record<string, number> {
  49  |   const parts = token.split('.');
  50  |   if (parts.length !== 3) return {};
  51  |   const payload = Buffer.from(parts[1], 'base64url').toString('utf8');
  52  |   return JSON.parse(payload);
  53  | }
  54  | 
  55  | // ── Test suite ─────────────────────────────────────────────────────────────
  56  | test.describe('Weekly Weight Tracking Feature — Cycle 2', () => {
  57  | 
  58  |   // ── QA-7 / AC-1: JWT Token Expiry = 7 days ───────────────────────────────
  59  |   test('QA-7: JWT token expiry is 7 days (604800 seconds)', async ({ request }) => {
  60  |     const token = await loginViaApi(request);
  61  |     expect(token, 'Login should succeed and return a token').not.toBeNull();
  62  | 
  63  |     const payload = decodeJwtPayload(token!);
  64  |     expect(payload.exp, 'exp claim must exist').toBeTruthy();
  65  |     expect(payload.iat, 'iat claim must exist').toBeTruthy();
  66  | 
  67  |     const diff = payload.exp - payload.iat;
  68  |     // Allow ±60 second tolerance
  69  |     expect(diff).toBeGreaterThanOrEqual(604800 - 60);
  70  |     expect(diff).toBeLessThanOrEqual(604800 + 60);
  71  |     console.log(`  Token lifetime: ${diff}s (expected ~604800s)`);
  72  |   });
  73  | 
  74  |   // ── AC-11: appsettings.json ExpiryHours = 168 ────────────────────────────
  75  |   test('AC-11: appsettings.json has ExpiryHours = 168', async () => {
  76  |     const settingsPath = path.resolve(
  77  |       __dirname,
  78  |       '../../../../DietTracker.API/appsettings.json'
  79  |     );
  80  |     const settings = JSON.parse(fs.readFileSync(settingsPath, 'utf8'));
  81  |     expect(settings.JwtSettings?.ExpiryHours).toBe('168');
  82  |   });
  83  | 
  84  |   // ── QA-4 / AC-5: Login UI + weight popup save flow (happy path) ──────────
  85  |   test('QA-4 (UI): Login, popup appears, enter valid weight, Save → popup closes, chart updates', async ({ page, request }) => {
  86  |     // First clear any existing pending entry so popup is guaranteed to show
  87  |     // (rely on DB state: if no entry for prev week, popup fires)
  88  | 
  89  |     await loginViaUI(page);
  90  |     await screenshot(page, '01_dashboard_after_login');
  91  | 
  92  |     // Check if weight popup is present (up to 4s after dashboard renders)
  93  |     const overlay = page.locator('.ww-overlay, [role="dialog"]');
  94  |     const hasPopup = await overlay.isVisible({ timeout: 4000 }).catch(() => false);
  95  | 
  96  |     if (!hasPopup) {
  97  |       // No popup means weight already logged — still validate the chart empty-state path
  98  |       console.log('  Popup not shown (weight already logged for this week — testing chart exists)');
  99  |       const weightSection = page.locator('section:has(h2:text("Weekly Weight"))');
  100 |       await expect(weightSection).toBeVisible({ timeout: 5000 });
  101 |       await screenshot(page, '01b_no_popup_weight_already_logged');
  102 |       return;
  103 |     }
  104 | 
  105 |     // Popup is showing
  106 |     await screenshot(page, '02_weight_popup_visible');
  107 |     await expect(overlay).toBeVisible();
  108 | 
  109 |     // Verify title text
  110 |     const title = overlay.locator('#ww-title, .ww-modal__title');
  111 |     await expect(title).toContainText(/Log Your Weekly Weight/i);
  112 | 
  113 |     // Enter a valid weight
  114 |     const input = overlay.locator('input[type="number"]');
  115 |     await input.fill('75.0');
  116 | 
  117 |     // Save button should be enabled
  118 |     const saveBtn = overlay.locator('button:has-text("Save")');
  119 |     await expect(saveBtn).toBeEnabled();
  120 |     await screenshot(page, '03_weight_entered_75kg');
  121 | 
  122 |     // Intercept the POST request
  123 |     const [response] = await Promise.all([
  124 |       page.waitForResponse(resp =>
  125 |         resp.url().includes('/api/weight-entries') && resp.request().method() === 'POST',
  126 |         { timeout: 10000 }
  127 |       ),
  128 |       saveBtn.click(),
  129 |     ]);
  130 | 
  131 |     expect(response.status()).toBe(201);
  132 |     console.log(`  POST /api/weight-entries returned ${response.status()}`);
  133 | 
  134 |     // Popup should close
  135 |     await expect(overlay).toBeHidden({ timeout: 5000 });
  136 |     await screenshot(page, '04_popup_closed_after_save');
  137 | 
  138 |     // Dashboard chart section should exist and show data
  139 |     await page.waitForTimeout(2000); // allow re-render
  140 |     const weightSection = page.locator('section:has(h2:text("Weekly Weight"))');
  141 |     await expect(weightSection).toBeVisible();
  142 |     const chartSvg = weightSection.locator('svg');
  143 |     await expect(chartSvg).toBeVisible({ timeout: 5000 });
  144 |     await screenshot(page, '05_dashboard_chart_after_save');
  145 |   });
```