const TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjZhYzUxM2U3MmY1YTFlYzg1MTY3NTNhZiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXBhLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBjYWZlbWFuYWdlbWVudC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AY2FmZW1hbmFnZW1lbnQuY29tIiwiZnVsbE5hbWUiOiJDxrB1IHRy4bqLbiBUcuG7iyBWacOqbiAoQWRtaW4pIiwic2hvcElkIjoiIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3OTQ4MTMxNzQsImlzcyI6ImNhZmUtbWFuYWdlbWVudCIsImF1ZCI6ImNhZmUtbWFuYWdlbWVudC1jbGllbnQifQ.8iR_-M4vpm8VDFlwPuOXMoKTaitgVmZ7iV0sTMzdwfI';
const API_BASE = 'http://localhost:7890/api/v1';

async function main() {
  const endpoints = [
    'http://localhost:7890/healthz',
    `${API_BASE}/shops`,
    `${API_BASE}/menu`,
    `${API_BASE}/orders`,
    `${API_BASE}/inventory`,
  ];

  for (const url of endpoints) {
    try {
      const res = await fetch(url, {
        headers: { 'Authorization': `Bearer ${TOKEN}` }
      });
      const text = await res.text();
      console.log(`${url}: ${res.status} ${res.statusText}`);
      if (text) console.log(`  Response: ${text.substring(0, 200)}`);
    } catch (e) {
      console.log(`${url}: ERROR - ${e.message}`);
    }
  }
}

main().catch(console.error);