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
  const menuItemButtons = await page.$$('button:has-text("Cà phê"), button:has-text("Trà"), button:has-text("Bạc xỉu"), button:has-text("Cappuccino")').catch(() => []);
  if (menuItemButtons.length > 0) {
    await menuItemButtons[0].click();
    await page.waitForTimeout(500);
    console.log('✅ Added menu item');
  }

  const customerNameInput = await page.$('input[placeholder="Anh Tuấn"], input[name="customerName"]').catch(() => null);
  if (customerNameInput) {
    await customerNameInput.fill('Test Customer');
    await customerNameInput.press('Tab');
    await page.waitForTimeout(200);
    console.log('✅ Filled customer name');
  }

  const submitBtn = await page.$('button:has-text("Hoàn tất"), button[type="submit"]').catch(() => null);
  if (submitBtn) {
    const isDisabled = await submitBtn.getAttribute('disabled');
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
    for (const o of items) {
      console.log(`  Order: ${o.orderNumber || o.id}, status: ${o.status}`);
    }
    await browser.close();
    return;
  }

  const frontendOrderNumber = `ORD-${pendingOrder.id.slice(-6).toUpperCase()}`;
  console.log(`Found pending order: ${frontendOrderNumber} (id: ${pendingOrder.id})`);

  // Reload orders page to get fresh data
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  // Step 1: Click "Pha chế" to move to Preparing
  console.log('\n=== Step 1: Move to Preparing ===');
  let orderRow = await page.$(`tr:has-text("${frontendOrderNumber}")`);
  if (orderRow) {
    const phaCheButton = await orderRow.$('button:has-text("Pha chế")');
    if (phaCheButton) {
      await phaCheButton.click();
      await page.waitForTimeout(3000);
      console.log('✅ Clicked "Pha chế"');
    }
  }

  // Step 2: Click "Báo xong" to move to Ready
  console.log('\n=== Step 2: Move to Ready ===');
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  orderRow = await page.$(`tr:has-text("${frontendOrderNumber}")`);
  if (orderRow) {
    const baoXongButton = await orderRow.$('button:has-text("Báo xong")');
    if (baoXongButton) {
      await baoXongButton.click();
      await page.waitForTimeout(3000);
      console.log('✅ Clicked "Báo xong"');
    } else {
      console.log('"Báo xong" button not found');
      const rowButtons = await orderRow.$$('button');
      for (const btn of rowButtons) {
        const text = await btn.textContent().catch(() => '');
        console.log(`  Row button: "${text.trim()}"`);
      }
    }
  }

  // Step 3: Click "Thu tiền & Hoàn tất" to open payment modal
  console.log('\n=== Step 3: Open Payment Modal ===');
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  orderRow = await page.$(`tr:has-text("${frontendOrderNumber}")`);
  if (orderRow) {
    const payButton = await orderRow.$('button:has-text("Thu tiền & Hoàn tất")');
    if (payButton) {
      await payButton.click();
      await page.waitForTimeout(2000);
      console.log('✅ Clicked "Thu tiền & Hoàn tất" - payment modal should be open');

      // Check for payment modal
      const modal = await page.$('[role="dialog"], .modal, .fixed.inset-0').catch(() => null);
      if (modal) {
        console.log('✅ Payment modal opened');

        // Get order total from modal
        const modalText = await modal.textContent().catch(() => '');
        console.log(`Modal content preview: ${modalText.substring(0, 300)}`);

        // Find cash input
        const cashInput = await page.$('input[type="number"], input[placeholder*="100000"]').catch(() => null);
        if (cashInput) {
          console.log('✅ Cash input found');

          // Get order total from API to know how much to pay
          const orderRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}`, {
            headers: { 'Authorization': `Bearer ${token}` }
          });
          const orderData = await orderRes.json();
          const total = orderData.totalAmount?.amount || 0;
          console.log(`Order total: ${total}`);

          // Fill in cash amount (total + some change)
          const payAmount = total + 10000; // Add 10000 for change
          await cashInput.fill(String(payAmount));
          await page.waitForTimeout(500);
          console.log(`✅ Filled cash amount: ${payAmount}`);

          // Press Enter to submit
          await cashInput.press('Enter');
          await page.waitForTimeout(5000);
          console.log('✅ Pressed Enter to submit payment');

          // Check if modal closed
          const modalStillOpen = await page.$('[role="dialog"], .modal, .fixed.inset-0').catch(() => null);
          if (!modalStillOpen) {
            console.log('✅ Payment modal closed - payment likely successful');
          } else {
            console.log('⚠️ Payment modal still open');
            const modalText2 = await modalStillOpen.textContent().catch(() => '');
            console.log(`Modal content: ${modalText2.substring(0, 500)}`);
          }
        } else {
          console.log('❌ Cash input not found in modal');
        }
      } else {
        console.log('❌ Payment modal did not open');
      }
    } else {
      console.log('"Thu tiền & Hoàn tất" button not found');
      const rowButtons = await orderRow.$$('button');
      for (const btn of rowButtons) {
        const text = await btn.textContent().catch(() => '');
        console.log(`  Row button: "${text.trim()}"`);
      }
    }
  }

  // Final check: verify order status
  console.log('\n=== Final Status Check ===');
  const checkRes = await fetch(`${API_BASE}/orders/${pendingOrder.id}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const checkData = await checkRes.json();
  const statusNames = ['Pending', 'Preparing', 'Ready', 'Completed', 'Cancelled'];
  const statusName = statusNames[checkData.status - 1] || `Unknown(${checkData.status})`;
  console.log(`Final order status: ${checkData.status} (${statusName})`);
  console.log(`Payment status: ${checkData.paymentStatus}`);

  await browser.close();
}

main().catch(console.error);