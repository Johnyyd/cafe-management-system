const TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjZhYzUxM2U3MmY1YTFlYzg1MTY3NTNhZiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXBhLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBjYWZlbWFuYWdlbWVudC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AY2FmZW1hbmFnZW1lbnQuY29tIiwiZnVsbE5hbWUiOiJDxrB1IHRy4bqLbiBUcuG7iyBWacOqbiAoQWRtaW4pIiwic2hvcElkIjoiIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3OTQ4MTI5NzgsImlzcyI6ImNhZmUtbWFuYWdlbWVudCIsImF1ZCI6ImNhZmUtbWFuYWdlbWVudC1jbGllbnQifQ.KP3QyR-f2PK5Qk3lB5VbnYNxGVboIiszG6ME_T_29E8';
const API_BASE = 'http://localhost:7890/api/v1';

async function fetchJson(url, options = {}) {
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${TOKEN}`,
      ...options.headers,
    },
  });
  const text = await res.text();
  return text ? JSON.parse(text) : {};
}

async function main() {
  // Get orders
  const orders = await fetchJson(`${API_BASE}/orders`);
  const items = Array.isArray(orders) ? orders : orders.items || [];
  console.log('Total orders:', items.length);

  const shopId = '6ad47943f0082e40f2303b17';
  const shopOrders = items.filter(o => o.shopId === shopId);
  console.log(`Orders for shop ${shopId}:`, shopOrders.length);

  shopOrders.forEach(o => {
    console.log(`  ${o.orderNumber || o.id}: status=${o.status} (${['Pending','Preparing','Ready','Completed','Cancelled'][o.status-1] || 'unknown'})`);
  });

  // Find a Preparing order (status 2) to move to Ready (status 3)
  const preparingOrder = shopOrders.find(o => o.status === 2);
  if (preparingOrder) {
    console.log('\nMoving order to Ready:', preparingOrder.orderNumber || preparingOrder.id);
    const res = await fetchJson(`${API_BASE}/orders/${preparingOrder.id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status: 3 }), // Ready = 3
    });
    console.log('Response:', res);
  }

  // Check again
  const orders2 = await fetchJson(`${API_BASE}/orders`);
  const items2 = Array.isArray(orders2) ? orders2 : orders2.items || [];
  const shopOrders2 = items2.filter(o => o.shopId === shopId);
  console.log('\nAfter update:');
  shopOrders2.forEach(o => {
    console.log(`  ${o.orderNumber || o.id}: status=${o.status} (${['Pending','Preparing','Ready','Completed','Cancelled'][o.status-1] || 'unknown'})`);
  });
}

main().catch(console.error);