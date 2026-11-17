const { chromium } = require('playwright');

const FRONTEND_URL = 'http://localhost:3000';
const API_BASE = 'http://localhost:7890/api/v1';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Listen for console messages
  page.on('console', msg => console.log(`[Console] ${msg.type()}: ${msg.text()}`));

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
  console.log(`Token obtained`);

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

  // Now test the frontend's api.updateOrderStatus function
  const result = await page.evaluate(async (orderId) => {
    // Import the api module - but we can't easily import ES modules in evaluate
    // Instead, let's check if the function exists on window or try to call it via the React component
    // Actually, let's just call the fetch directly with the same logic as the frontend

    const token = localStorage.getItem('cms_token');
    const statusMap = {
      Pending: 1,
      Confirmed: 1,
      Preparing: 2,
      Ready: 3,
      Completed: 4,
      Cancelled: 5,
    };
    const statusCode = statusMap['Preparing'] ?? 1;

    const response = await fetch(`/api/v1/orders/${orderId}/status`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status: statusCode }),
    });

    return { status: response.status, ok: response.ok };
  }, pendingOrder.id);

  console.log(`Frontend-style API call result:`, result);

  // Check order status again
  const checkRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const checkData = await checkRes.json();
  console.log(`Order after update: status=${checkData.status}`);

  await browser.close();
}

main().catch(console.error);