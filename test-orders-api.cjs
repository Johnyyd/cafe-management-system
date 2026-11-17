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
  console.log(`Token: ${token.substring(0, 50)}...`);

  // Call orders API
  const res = await fetch(`${API_BASE}/orders`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await res.json();
  const items = Array.isArray(data) ? data : data.items || [];
  console.log('Total orders:', items.length);
  items.forEach(o => console.log(`Order: ${o.orderNumber || o.id}, status: ${o.status}, type: ${typeof o.status}`));

  await browser.close();
}

main().catch(console.error);