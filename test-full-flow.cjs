const { chromium } = require('playwright');

const FRONTEND_URL = 'http://localhost:3000';
const API_BASE = 'http://localhost:7890/api/v1';

async function main() {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Listen for console messages
  page.on('console', msg => console.log(`[Console] ${msg.type()}: ${msg.text()}`));
  page.on('pageerror', err => console.log(`[PageError] ${err.message}`));

  // Login
  await page.goto(`${FRONTEND_URL}/login`, { waitUntil: 'networkidle', timeout: 30000 });
  await page.waitForTimeout(1000);

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

  await page.click('button[type="submit"], button:has-text("Đăng nhập"), button:has-text("Login")');
  await page.waitForURL('**/dashboard', { timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(2000);

  // Go to orders page
  await page.goto(`${FRONTEND_URL}/orders`, { waitUntil: 'networkidle', timeout: 15000 });
  await page.waitForTimeout(3000);

  // Create a new order via POS form
  // 1. Click a menu item
  const menuItemButtons = await page.$$('button:has-text("Cà phê"), button:has-text("Trà"), button:has-text("Bạc xỉu"), button:has-text("Cappuccino")').catch(() => []);
  if (menuItemButtons.length > 0) {
    await menuItemButtons[0].click();
    await page.waitForTimeout(500);
    console.log('✅ Added menu item');
  }

  // 2. Fill customer name
  const customerNameInput = await page.$('input[placeholder="Anh Tuấn"], input[name="customerName"]').catch(() => null);
  if (customerNameInput) {
    await customerNameInput.fill('Test Customer');
    await customerNameInput.press('Tab');
    await page.waitForTimeout(200);
    console.log('✅ Filled customer name');
  }

  // 3. Submit order
  const submitBtn = await page.$('button:has-text("Hoàn tất"), button[type="submit"]').catch(() => null);
  if (submitBtn) {
    const isDisabled = await submitBtn.getAttribute('disabled');
    console.log(`Submit button disabled: ${isDisabled}`);
    if (!isDisabled) {
      await submitBtn.click();
      await page.waitForTimeout(5000);
      console.log('✅ Submitted order');
    }
  }

  // Get token from localStorage
  const token = await page.evaluate(() => localStorage.getItem('cms_token'));

  // Get orders to find the newly created Pending one
  const ordersRes = await fetch(`${API_BASE}/orders?pageSize=10`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const ordersData = await ordersRes.json();
  const items = Array.isArray(ordersData) ? ordersData : ordersData.items || [];
  const pendingOrder = items.find(o => o.status === 1);

  if (!pendingOrder) {
    console.log('No pending order found after creation');
    // List all orders
    for (const o of items) {
      console.log(`  Order: ${o.orderNumber || o.id}, status: ${o.status}`);
    }
    await browser.close();
    return;
  }

  // Frontend generates orderNumber as ORD- + last 6 chars of ID
  const frontendOrderNumber = `ORD-${pendingOrder.id.slice(-6).toUpperCase()}`;
  console.log(`Found pending order: ${frontendOrderNumber} (id: ${pendingOrder.id})`);

  // Reload orders page to get fresh data
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  // Find the "Pha chế" button for this order
  const orderRow = await page.$(`tr:has-text("${frontendOrderNumber}")`);
  if (orderRow) {
    console.log('Found order row');
    const phaCheButton = await orderRow.$('button:has-text("Pha chế")');
    if (phaCheButton) {
      console.log('Found "Pha chế" button');

      // Check if button is disabled
      const isDisabled = await phaCheButton.getAttribute('disabled');
      console.log(`Button disabled: ${isDisabled}`);

      // Listen for network requests
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

      // Click the button
      console.log('Clicking button...');
      await phaCheButton.click();

      // Wait and check what happens
      await page.waitForTimeout(5000);

      // Check URL
      const url = page.url();
      console.log(`URL after click: ${url}`);

      // Check for buttons in table
      const buttons = await page.$$('table button');
      console.log(`Buttons in table after click: ${buttons.length}`);
      for (const btn of buttons) {
        const text = await btn.textContent().catch(() => '');
        console.log(`  Button: "${text.trim()}"`);
      }

      // Check order status via API
      const checkRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      const checkData = await checkRes.json();
      console.log(`Order status after click: ${checkData.status}`);
    } else {
      console.log('"Pha chế" button not found in row');
      // Check what buttons are in the row
      const rowButtons = await orderRow.$$('button');
      for (const btn of rowButtons) {
        const text = await btn.textContent().catch(() => '');
        console.log(`  Row button: "${text.trim()}"`);
      }
    }
  } else {
    console.log('Order row not found');
  }

  await browser.close();
}

main().catch(console.error);