const { chromium } = require('playwright');

const FRONTEND_URL = 'http://localhost:3000';
const API_BASE = 'http://localhost:7890/api/v1';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

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

  // Get token from localStorage
  const token = await page.evaluate(() => localStorage.getItem('cms_token'));
  console.log(`Token obtained`);

  // Get orders to find a Pending one
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

  // Try to update status directly via API
  const updateRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}/status`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ status: 2 }) // Preparing = 2
  });

  console.log(`Update status response: ${updateRes.status} ${updateRes.statusText}`);
  const updateBody = await updateRes.text();
  console.log(`Response body: ${updateBody}`);

  // Check order status again
  const checkRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const checkData = await checkRes.json();
  console.log(`Order after update: status=${checkData.status}`);

  await browser.close();
}

main().catch(console.error);