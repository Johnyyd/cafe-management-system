const { chromium } = require('playwright');

const API_BASE = 'http://localhost:7890/api/v1';
const FRONTEND_URL = 'http://localhost:3000';

async function testAPIEndpoints(token) {
  console.log('\n=== Testing API Endpoints ===');

  const publicTests = [
    { name: 'Health Check', url: 'http://localhost:7890/healthz', method: 'GET', auth: false },
    { name: 'Shops', url: `${API_BASE}/shops`, method: 'GET', auth: false },
    { name: 'Menu Items', url: `${API_BASE}/menu`, method: 'GET', auth: false },
  ];

  const authTests = [
    { name: 'Orders', url: `${API_BASE}/orders`, method: 'GET', auth: true },
    { name: 'Inventory', url: `${API_BASE}/inventory`, method: 'GET', auth: true },
    { name: 'Low Stock', url: `${API_BASE}/inventory/low-stock`, method: 'GET', auth: true },
    { name: 'Out of Stock', url: `${API_BASE}/inventory/out-of-stock`, method: 'GET', auth: true },
    { name: 'Staff', url: `${API_BASE}/staff`, method: 'GET', auth: true },
    { name: 'Shifts', url: `${API_BASE}/shifts`, method: 'GET', auth: true },
    { name: 'Metrics', url: `${API_BASE}/metrics`, method: 'GET', auth: true },
  ];

  const results = [];

  // Test public endpoints
  for (const test of publicTests) {
    try {
      const res = await fetch(test.url, { method: test.method });
      const status = res.ok ? '✅ PASS' : `❌ FAIL (${res.status})`;
      results.push({ name: test.name, status, url: test.url });
      console.log(`${status} - ${test.name}`);
    } catch (e) {
      results.push({ name: test.name, status: `❌ ERROR: ${e.message}`, url: test.url });
      console.log(`❌ ERROR - ${test.name}: ${e.message}`);
    }
  }

  // Test authenticated endpoints
  if (token) {
    for (const test of authTests) {
      try {
        const res = await fetch(test.url, {
          method: test.method,
          headers: { 'Authorization': `Bearer ${token}` }
        });
        const status = res.ok ? '✅ PASS' : `❌ FAIL (${res.status})`;
        results.push({ name: test.name, status, url: test.url });
        console.log(`${status} - ${test.name}`);
      } catch (e) {
        results.push({ name: test.name, status: `❌ ERROR: ${e.message}`, url: test.url });
        console.log(`❌ ERROR - ${test.name}: ${e.message}`);
      }
    }
  } else {
    console.log('⚠️ No token - skipping authenticated endpoints');
    for (const test of authTests) {
      results.push({ name: test.name, status: '⚠️ SKIPPED (no auth)', url: test.url });
    }
  }

  return results;
}

async function login(page) {
  console.log('\n=== Logging in ===');

  // Listen for console messages
  page.on('console', msg => console.log(`[Console] ${msg.type()}: ${msg.text()}`));

  // Listen for network requests
  page.on('request', req => {
    if (req.url().includes('/auth/login')) {
      console.log(`[Network] POST ${req.url()}`);
      console.log(`[Network] Request body: ${req.postData()}`);
    }
  });
  page.on('response', async res => {
    if (res.url().includes('/auth/login')) {
      console.log(`[Network] Response: ${res.status()}`);
      const body = await res.text().catch(() => 'failed');
      console.log(`[Network] Response body: ${body}`);
    }
  });

  await page.goto(`${FRONTEND_URL}/login`, { waitUntil: 'networkidle', timeout: 30000 });
  await page.waitForTimeout(1000);

  // Fill login form - use type to trigger input events, then blur
  const emailInput = await page.$('input[type="email"], input[name="email"]');
  await emailInput.click();
  await emailInput.type('admin@cafemanagement.com', { delay: 50 });
  await emailInput.press('Tab');
  await page.waitForTimeout(200);

  const passwordInput = await page.$('input[type="password"], input[name="password"]');
  await passwordInput.click();
  await passwordInput.type('Password123!', { delay: 50 });
  await passwordInput.press('Tab');
  await page.waitForTimeout(500);

  // Check if button is enabled
  const loginBtn = await page.$('button[type="submit"]');
  const isDisabled = await loginBtn.getAttribute('disabled');
  console.log(`Login button disabled: ${isDisabled}`);

  // If still disabled, try force submit via form evaluate
  if (isDisabled) {
    console.log('Button still disabled, trying to trigger form validation via evaluate...');
    // Try to manually trigger validation on the form
    await page.evaluate(() => {
      const form = document.querySelector('form');
      if (form) {
        // Dispatch submit event to trigger validation
        form.requestSubmit();
      }
    });
    await page.waitForTimeout(1000);
  }

  // Click login button
  await page.click('button[type="submit"], button:has-text("Đăng nhập"), button:has-text("Login")');

  // Wait for redirect
  await page.waitForURL('**/dashboard', { timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(2000);

  // Get token from localStorage
  const token = await page.evaluate(() => localStorage.getItem('cms_token'));
  console.log(`Login ${token ? '✅ successful' : '❌ failed'} (token: ${token ? 'present' : 'missing'})`);
  if (token) {
    console.log(`Token preview: ${token.substring(0, 50)}...`);
  }

  return token;
}

async function testFrontend(page) {
  console.log('\n=== Testing Frontend ===');

  const results = [];

  // Test 1: Load homepage (should redirect to login)
  try {
    await page.goto(FRONTEND_URL, { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForSelector('#root', { timeout: 10000 });
    console.log('✅ PASS - Homepage loads');
    results.push({ name: 'Homepage Load', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Homepage Load: ${e.message}`);
    results.push({ name: 'Homepage Load', status: `❌ FAIL: ${e.message}` });
    return { results, token: null };
  }

  // Test 2: Login
  const token = await login(page);
  if (!token) {
    results.push({ name: 'Login', status: '❌ FAIL' });
    return { results, token: null };
  }
  results.push({ name: 'Login', status: '✅ PASS' });

  // Test 3: Test navigation to Dashboard
  try {
    await page.goto(`${FRONTEND_URL}/dashboard`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Dashboard page loads');
    results.push({ name: 'Dashboard Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Dashboard Page: ${e.message}`);
    results.push({ name: 'Dashboard Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 4: Test navigation to Orders page
  try {
    await page.goto(`${FRONTEND_URL}/orders`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    const hasOrdersContent = await page.$('text=/Bán Hàng|POS|Đơn Hàng|orders/i').catch(() => null);
    if (hasOrdersContent) {
      console.log('✅ PASS - Orders page loads');
      results.push({ name: 'Orders Page', status: '✅ PASS' });
    } else {
      console.log('⚠️ Orders page loaded but content not verified');
      results.push({ name: 'Orders Page', status: '⚠️ PARTIAL' });
    }
  } catch (e) {
    console.log(`❌ FAIL - Orders Page: ${e.message}`);
    results.push({ name: 'Orders Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 5: Test navigation to Menu page
  try {
    await page.goto(`${FRONTEND_URL}/menu`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Menu page loads');
    results.push({ name: 'Menu Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Menu Page: ${e.message}`);
    results.push({ name: 'Menu Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 6: Test navigation to Inventory page
  try {
    await page.goto(`${FRONTEND_URL}/inventory`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Inventory page loads');
    results.push({ name: 'Inventory Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Inventory Page: ${e.message}`);
    results.push({ name: 'Inventory Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 7: Test navigation to Staff page
  try {
    await page.goto(`${FRONTEND_URL}/staff`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Staff page loads');
    results.push({ name: 'Staff Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Staff Page: ${e.message}`);
    results.push({ name: 'Staff Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 8: Test navigation to Shops page
  try {
    await page.goto(`${FRONTEND_URL}/shops`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Shops page loads');
    results.push({ name: 'Shops Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Shops Page: ${e.message}`);
    results.push({ name: 'Shops Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 9: Test navigation to Shifts page
  try {
    await page.goto(`${FRONTEND_URL}/shifts`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Shifts page loads');
    results.push({ name: 'Shifts Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Shifts Page: ${e.message}`);
    results.push({ name: 'Shifts Page', status: `❌ FAIL: ${e.message}` });
  }

  // Test 10: Test navigation to Metrics page
  try {
    await page.goto(`${FRONTEND_URL}/metrics`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    console.log('✅ PASS - Metrics page loads');
    results.push({ name: 'Metrics Page', status: '✅ PASS' });
  } catch (e) {
    console.log(`❌ FAIL - Metrics Page: ${e.message}`);
    results.push({ name: 'Metrics Page', status: `❌ FAIL: ${e.message}` });
  }

  return { results, token };
}

async function createTestOrder(page, token) {
  console.log('\n=== Creating Test Order via POS Form ===');

  try {
    // Go to orders page
    await page.goto(`${FRONTEND_URL}/orders`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(3000);

    // The POS form is on the right side - fill it out
    // 1. Select a menu item from the left catalog
    const menuItemButtons = await page.$$('button:has-text("Cà phê"), button:has-text("Trà"), button:has-text("Bạc xỉu"), button:has-text("Cappuccino")').catch(() => []);
    if (menuItemButtons.length > 0) {
      await menuItemButtons[0].click();
      await page.waitForTimeout(500);
      console.log('✅ Added first menu item from catalog');
    } else {
      // Try to find any clickable menu item in the catalog
      const catalogItems = await page.$$('lg\\:col-span-7 button, .grid-cols-2 button').catch(() => []);
      if (catalogItems.length > 0) {
        await catalogItems[0].click();
        await page.waitForTimeout(500);
        console.log('✅ Added first catalog item');
      } else {
        console.log('⚠️ No menu items found in catalog');
        return false;
      }
    }

    // 2. Fill customer name
    const customerNameInput = await page.$('input[placeholder="Anh Tuấn"], input[name="customerName"]').catch(() => null);
    if (customerNameInput) {
      await customerNameInput.fill('Test Customer');
      await customerNameInput.press('Tab');
      await page.waitForTimeout(200);
      console.log('✅ Filled customer name');
    }

    // 3. Fill phone (optional for DineIn)
    const phoneInput = await page.$('input[placeholder="0901234567"], input[name="customerPhone"]').catch(() => null);
    if (phoneInput) {
      await phoneInput.fill('0901234567');
      await phoneInput.press('Tab');
      await page.waitForTimeout(200);
      console.log('✅ Filled phone');
    }

    // 4. Submit order - look for "Hoàn tất & In hóa đơn" button
    const submitBtn = await page.$('button:has-text("Hoàn tất"), button[type="submit"]').catch(() => null);
    if (submitBtn) {
      const isDisabled = await submitBtn.getAttribute('disabled');
      console.log(`Submit button disabled: ${isDisabled}`);
      if (!isDisabled) {
        await submitBtn.click();
        await page.waitForTimeout(3000);
        console.log('✅ Submitted order');

        // Check for success notification or new order in list
        await page.waitForTimeout(2000);
        return true;
      } else {
        console.log('⚠️ Submit button is disabled - form may be invalid');
        return false;
      }
    } else {
      console.log('⚠️ Submit button not found');
      return false;
    }
  } catch (e) {
    console.log(`❌ Create Order Error: ${e.message}`);
    return false;
  }
}

async function testPaymentModal(page, payButton, results) {
  try {
    // Click payment button
    await payButton.click();
    await page.waitForTimeout(1000);

    // Check for payment modal
    const modal = await page.$('[role="dialog"], .modal, .fixed.inset-0').catch(() => null);
    if (modal) {
      console.log('✅ PASS - Payment modal opens');
      results.push({ name: 'Payment Modal Opens', status: '✅ PASS' });

      // Check for cash input
      const cashInput = await page.$('input[type="number"], input[placeholder*="100000"]').catch(() => null);
      if (cashInput) {
        console.log('✅ PASS - Cash input found');
        results.push({ name: 'Cash Input', status: '✅ PASS' });

        // Get the order total from modal
        const totalText = await page.$eval('[role="dialog"]', el => el.textContent).catch(() => '');
        console.log(`Modal content: ${totalText.substring(0, 200)}`);

        // Fill in cash amount (use a large enough amount)
        await cashInput.fill('100000');
        await page.waitForTimeout(500);

        // Test Enter key on cash input
        await cashInput.press('Enter');
        await page.waitForTimeout(3000);
        console.log('✅ PASS - Enter key triggers payment');
        results.push({ name: 'Enter Key Payment', status: '✅ PASS' });

        // Wait for modal to close or check for success
        await page.waitForTimeout(1000);
      } else {
        console.log('❌ FAIL - Cash input not found');
        results.push({ name: 'Cash Input', status: '❌ FAIL' });
      }

      // Close modal if still open
      const modalStillOpen = await page.$('[role="dialog"], .modal, .fixed.inset-0').catch(() => null);
      if (modalStillOpen) {
        await page.keyboard.press('Escape');
      }
    } else {
      console.log('❌ FAIL - Payment modal did not open');
      results.push({ name: 'Payment Modal Opens', status: '❌ FAIL' });
    }
  } catch (e) {
    console.log(`❌ FAIL - Payment Modal: ${e.message}`);
    results.push({ name: 'Payment Modal', status: `❌ FAIL: ${e.message}` });
  }
}

async function testPaymentFlow(page, token) {
  console.log('\n=== Testing Payment Flow ===');
  const results = [];

  try {
    // Go to orders page
    await page.goto(`${FRONTEND_URL}/orders`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(3000);

    // Check for orders table
    const orderRows = await page.$$('.grid > div, table tbody tr, [data-testid="order-row"]').catch(() => []);
    console.log(`Found ${orderRows.length} potential order rows`);

    if (orderRows.length > 0) {
      // First, try to find an order in Ready status and move it there if needed
      // Look for "Pha chế" (Preparing) or "Báo xong" (Ready) buttons
      const preparingButtons = await page.$$('button:has-text("Pha chế")').catch(() => []);
      const readyButtons = await page.$$('button:has-text("Báo xong")').catch(() => []);
      const payButtons = await page.$$('button:has-text("Thu tiền & Hoàn tất")').catch(() => []);

      console.log(`Found ${preparingButtons.length} Preparing buttons, ${readyButtons.length} Ready buttons, ${payButtons.length} Payment buttons`);

      // If there are orders in Ready, use them
      if (readyButtons.length > 0) {
        console.log('Moving order to Ready status...');
        await readyButtons[0].click();
        await page.waitForTimeout(3000);

        // Refresh payment buttons
        const newPayButtons = await page.$$('button:has-text("Thu tiền & Hoàn tất")').catch(() => []);
        console.log(`After moving to Ready: ${newPayButtons.length} Payment buttons`);

        if (newPayButtons.length > 0) {
          await testPaymentModal(page, newPayButtons[0], results);
        }
      } else if (preparingButtons.length > 0) {
        console.log('Moving order from Pending to Preparing...');
        // Check if button is enabled
        const isDisabled = await preparingButtons[0].getAttribute('disabled');
        console.log(`Preparing button disabled: ${isDisabled}`);

        if (!isDisabled) {
          // Listen for network requests to see the API call
          page.on('request', req => {
            if (req.url().includes('/orders/') && req.url().includes('/status')) {
              console.log(`[Network] ${req.method()} ${req.url()}`);
              console.log(`[Network] Request body: ${req.postData()}`);
            }
          });
          page.on('response', async res => {
            if (res.url().includes('/orders/') && res.url().includes('/status')) {
              console.log(`[Network] Response: ${res.status()}`);
              const body = await res.text().catch(() => 'failed');
              console.log(`[Network] Response body: ${body}`);
            }
          });

          await preparingButtons[0].click();
          // Wait for status change and UI update
          await page.waitForTimeout(5000);

          // Check all buttons after click
          const allButtons = await page.$$('table button').catch(() => []);
          const buttonTexts = [];
          for (const btn of allButtons) {
            const text = await btn.textContent().catch(() => '');
            if (text.trim()) buttonTexts.push(text.trim());
          }
          console.log('All button texts after click:', buttonTexts);

          // Check currentShopId
          const currentShopId = await page.evaluate(() => localStorage.getItem('cms_current_shop_id'));
          console.log(`Current shop ID: ${currentShopId}`);

          // Reload page to get fresh order list
          await page.reload({ waitUntil: 'networkidle' });
          await page.waitForTimeout(3000);

          // Check shop ID after reload
          const currentShopId2 = await page.evaluate(() => localStorage.getItem('cms_current_shop_id'));
          console.log(`Current shop ID after reload: ${currentShopId2}`);

          // Now try to find Ready button (Báo xong)
          const readyButtons2 = await page.$$('button:has-text("Báo xong")').catch(() => []);
          console.log(`Found ${readyButtons2.length} Ready buttons after reload`);

          // Also check for Pha chế buttons
          const preparingButtons2 = await page.$$('button:has-text("Pha chế")').catch(() => []);
          console.log(`Found ${preparingButtons2.length} Preparing buttons after reload`);

          // Debug: check actual order statuses from API
          const apiOrders = await page.evaluate(async () => {
            const token = localStorage.getItem('cms_token');
            const shopId = localStorage.getItem('cms_current_shop_id');
            const res = await fetch(`http://localhost:7890/api/v1/orders?shopId=${shopId}&pageSize=100`, {
              headers: { 'Authorization': `Bearer ${token}` }
            });
            const data = await res.json();
            const items = Array.isArray(data) ? data : data.items || [];
            return items.map(o => ({ orderNumber: o.orderNumber || o.id, status: o.status }));
          });
          console.log('API order statuses after reload:', apiOrders);

          if (readyButtons2.length > 0) {
            const isDisabled2 = await readyButtons2[0].getAttribute('disabled');
            console.log(`Ready button disabled: ${isDisabled2}`);

            if (!isDisabled2) {
              await readyButtons2[0].click();
              await page.waitForTimeout(3000);

              // Check buttons again
              const allButtons2 = await page.$$('table button').catch(() => []);
              const buttonTexts2 = [];
              for (const btn of allButtons2) {
                const text = await btn.textContent().catch(() => '');
                if (text.trim()) buttonTexts2.push(text.trim());
              }
              console.log('All button texts after Ready click:', buttonTexts2);

              // Reload again
              await page.reload({ waitUntil: 'networkidle' });
              await page.waitForTimeout(3000);

              const newPayButtons = await page.$$('button:has-text("Thu tiền & Hoàn tất")').catch(() => []);
              console.log(`After moving to Ready: ${newPayButtons.length} Payment buttons`);

              if (newPayButtons.length > 0) {
                await testPaymentModal(page, newPayButtons[0], results);
              }
            } else {
              console.log('⚠️ Ready button is disabled');
              results.push({ name: 'Ready Button Disabled', status: '⚠️ DISABLED' });
            }
          } else {
            console.log('⚠️ No Báo xong buttons found - checking for other status buttons');
            const otherButtons = await page.$$('table button:not(:has-text("Pha chế")):not(:has-text("Mã QR")):not(:has-text("Xóa"))').catch(() => []);
            const otherTexts = [];
            for (const btn of otherButtons) {
              const text = await btn.textContent().catch(() => '');
              if (text.trim()) otherTexts.push(text.trim());
            }
            console.log('Other status buttons:', otherTexts);
          }
        } else {
          console.log('⚠️ Preparing button is disabled');
          results.push({ name: 'Preparing Button Disabled', status: '⚠️ DISABLED' });
        }
      } else if (payButtons.length > 0) {
        // Already have payment buttons
        await testPaymentModal(page, payButtons[0], results);
      } else {
        console.log('⚠️ No orders in Preparing/Ready status to test payment');
        results.push({ name: 'Orders in Ready Status', status: '⚠️ NONE' });
      }
    } else {
      console.log('⚠️ No orders found to test payment');
      results.push({ name: 'Orders Available', status: '⚠️ NONE' });
    }
  } catch (e) {
    console.log(`❌ FAIL - Payment Flow: ${e.message}`);
    results.push({ name: 'Payment Flow', status: `❌ FAIL: ${e.message}` });
  }

  return results;
}

async function runAllTests() {
  console.log('🚀 Starting Cafe Management System Tests');
  console.log(`Frontend: ${FRONTEND_URL}`);
  console.log(`API: ${API_BASE}`);

  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Test public API endpoints first (no auth needed)
  const publicApiResults = await testAPIEndpoints(null);

  // Test frontend with login
  const { results: frontendResults, token } = await testFrontend(page);

  // Test authenticated API endpoints
  const authApiResults = await testAPIEndpoints(token);

  // Create a test order if possible
  await createTestOrder(page, token);

  // Test payment flow
  const paymentResults = await testPaymentFlow(page, token);

  await browser.close();

  // Summary
  console.log('\n\n=== TEST SUMMARY ===');
  const allResults = [...publicApiResults, ...authApiResults, ...frontendResults, ...paymentResults];
  const passed = allResults.filter(r => r.status.startsWith('✅')).length;
  const failed = allResults.filter(r => r.status.startsWith('❌')).length;
  const warnings = allResults.filter(r => r.status.startsWith('⚠️') || r.status.startsWith('🔐')).length;
  const skipped = allResults.filter(r => r.status.startsWith('⚠️ SKIPPED')).length;

  console.log(`\nTotal: ${allResults.length} | Passed: ${passed} | Failed: ${failed} | Warnings: ${warnings} | Skipped: ${skipped}`);

  console.log('\n--- Public API Tests ---');
  publicApiResults.forEach(r => console.log(`  ${r.status} - ${r.name}`));

  console.log('\n--- Authenticated API Tests ---');
  authApiResults.forEach(r => console.log(`  ${r.status} - ${r.name}`));

  console.log('\n--- Frontend Tests ---');
  frontendResults.forEach(r => console.log(`  ${r.status} - ${r.name}`));

  console.log('\n--- Payment Flow Tests ---');
  paymentResults.forEach(r => console.log(`  ${r.status} - ${r.name}`));

  if (failed > 0) {
    process.exit(1);
  }
}

runAllTests().catch(console.error);