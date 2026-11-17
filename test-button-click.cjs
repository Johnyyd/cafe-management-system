const { chromium } = require('playwright');

const FRONTEND_URL = 'http://localhost:3000';
const API_BASE = 'http://localhost:7890/api/v1';

async function main() {
  const browser = await chromium.launch({ headless: false }); // Run headed to see what happens
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

  // Get token from localStorage
  const token = await page.evaluate(() => localStorage.getItem('cms_token'));

  // Get orders to find a Pending one via API
  const ordersRes = await fetch(`${API_BASE}/orders?pageSize=10`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const ordersData = await ordersRes.json();
  const items = Array.isArray(ordersData) ? ordersData : ordersData.items || [];
  const pendingOrder = items.find(o => o.status === 1);

  if (!pendingOrder) {
    console.log('No pending order found');
    await browser.close();
    return;
  }

  console.log(`Found pending order: ${pendingOrder.orderNumber || pendingOrder.id} (id: ${pendingOrder.id})`);

  // Find the "Pha chế" button for this order
  // The button is in a table row - let's find it by looking for the order number
  const orderRow = await page.$(`tr:has-text("${pendingOrder.orderNumber || pendingOrder.id}")`);
  if (orderRow) {
    console.log('Found order row');
    const phaCheButton = await orderRow.$('button:has-text("Pha chế")');
    if (phaCheButton) {
      console.log('Found "Pha chế" button');

      // Check if button is disabled
      const isDisabled = await phaCheButton.getAttribute('disabled');
      console.log(`Button disabled: ${isDisabled}`);

      // Click the button
      console.log('Clicking button...');
      await phaCheButton.click();

      // Wait and check what happens
      await page.waitForTimeout(5000);

      // Check URL
      const url = page.url();
      console.log(`URL after click: ${url}`);

      // Check table content
      const tableHtml = await page.$eval('table', el => el.outerHTML).catch(() => 'No table found');
      console.log(`Table HTML length: ${tableHtml.length}`);

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
    }
  } else {
    console.log('Order row not found');
  }

  await browser.close();
}

main().catch(console.error);