// Fixed load for spike-metrics-mcp task 3.1: 20 normal, 5 slow, 5 failing requests to probe-web.
// Usage: node load.mjs [baseUrl]   (default http://localhost:5172, probe-web's http endpoint)
// Requests run one at a time so phases do not overlap in the metrics.

const baseUrl = (process.argv[2] ?? 'http://localhost:5172').replace(/\/$/, '');

const phases = [
  { name: 'normal', count: 20, path: '/' },
  { name: 'slow', count: 5, path: '/?slow=2000' },
  { name: 'fail', count: 5, path: '/?fail=true' },
];

for (const phase of phases) {
  const start = new Date();
  const statuses = {};
  let errorPages = 0;
  for (let i = 0; i < phase.count; i++) {
    const res = await fetch(baseUrl + phase.path);
    const body = await res.text();
    statuses[res.status] = (statuses[res.status] ?? 0) + 1;
    // probe-web returns 200 with an error message when probe-api fails.
    if (body.includes('id="error"')) errorPages++;
  }
  const end = new Date();
  console.log(JSON.stringify({
    phase: phase.name,
    requests: phase.count,
    startUtc: start.toISOString(),
    endUtc: end.toISOString(),
    webStatuses: statuses,
    errorPages,
  }));
}

// probe-web's resilience handler retries a failed API call 3 times (spike-aspire-probe findings),
// so each failing web request is 4 probe-api requests.
console.log('Expected probe-api requests: 20 normal + 5 slow + 5 x 4 fail = 45 (25 OK, 20 x 500).');
