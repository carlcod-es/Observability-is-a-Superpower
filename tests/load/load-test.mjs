// Load test for the hosted demo site. Node 18+, no dependencies.
// Usage: node tests/load/load-test.mjs [--label before-fix] [--compare tests/load/results/<run>.json]
//        [--users 10] [--duration 60] [--ramp 10] [--seed 1] [--timeout 30000] [--base https://observability.carlcod.es]
// Writes tests/load/results/<run-id>.json and .md. Same options and seed give the same page sequence per user.

import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const args = Object.fromEntries(
  process.argv.slice(2).reduce((pairs, arg, i, all) => {
    if (arg.startsWith('--')) pairs.push([arg.slice(2), all[i + 1]]);
    return pairs;
  }, [])
);

const config = {
  base: (args.base ?? 'https://observability.carlcod.es').replace(/\/$/, ''),
  users: Number(args.users ?? 10),
  durationSec: Number(args.duration ?? 60),
  rampSec: Number(args.ramp ?? 10),
  timeoutMs: Number(args.timeout ?? 30000),
  seed: Number(args.seed ?? 1),
};
const label = args.label ?? 'run';
const startedAt = new Date();
const runId = `${startedAt.toISOString().replace(/[:.]/g, '-')}-${label}`;
const userAgent = `SuperPower-LoadTest/1.0 (run ${runId})`;
const resultsDir = join(dirname(fileURLToPath(import.meta.url)), 'results');

const paths = [
  '/',
  '/about-us/',
  '/services/',
  '/services/seo-audit/',
  '/services/adwords-audit/',
  '/events/',
  '/events/how-to-get-the-most-out-of-your-cms/',
  '/events/upgrading-when-is-the-best-time/',
  '/events/migrating-to-umbraco-from-other-cmss/',
  '/contact-us/',
];

// Seeded PRNG (mulberry32) so each user requests pages in the same order every run
function random(seed) {
  return () => {
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const stats = new Map(paths.map(p => [p, { times: [], statuses: {}, errors: {} }]));
const start = performance.now();
const endAt = start + config.durationSec * 1000;

async function hit(path) {
  const s = stats.get(path);
  const t0 = performance.now();
  try {
    const res = await fetch(config.base + path, {
      headers: { 'User-Agent': userAgent },
      signal: AbortSignal.timeout(config.timeoutMs),
    });
    await res.arrayBuffer(); // include body download in the timing
    s.times.push(performance.now() - t0);
    s.statuses[res.status] = (s.statuses[res.status] ?? 0) + 1;
  } catch (err) {
    const key = err.name === 'TimeoutError' ? 'timeout' : (err.cause?.code ?? err.name);
    s.errors[key] = (s.errors[key] ?? 0) + 1;
  }
}

async function virtualUser(index) {
  const next = random(config.seed * 1000 + index);
  // Stagger start-up across the ramp period
  await new Promise(r => setTimeout(r, (config.rampSec * 1000 * index) / config.users));
  while (performance.now() < endAt) {
    await hit(paths[Math.floor(next() * paths.length)]);
  }
}

function percentile(sorted, p) {
  if (sorted.length === 0) return null;
  return sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)];
}

function summarise(page, times, statuses, errors) {
  const sorted = [...times].sort((a, b) => a - b);
  const errorCount = Object.values(errors).reduce((a, b) => a + b, 0);
  const non2xx = Object.entries(statuses)
    .filter(([code]) => !code.startsWith('2'))
    .reduce((sum, [, n]) => sum + n, 0);
  const r = n => (n == null ? null : Math.round(n));
  return {
    page,
    requests: sorted.length + errorCount,
    failed: errorCount + non2xx,
    minMs: r(sorted[0]),
    avgMs: r(sorted.length ? sorted.reduce((a, b) => a + b, 0) / sorted.length : null),
    p50Ms: r(percentile(sorted, 50)),
    p95Ms: r(percentile(sorted, 95)),
    p99Ms: r(percentile(sorted, 99)),
    maxMs: r(sorted.at(-1)),
  };
}

const columns = ['page', 'requests', 'failed', 'minMs', 'avgMs', 'p50Ms', 'p95Ms', 'p99Ms', 'maxMs'];

function markdownTable(headers, rows) {
  return [
    `| ${headers.join(' | ')} |`,
    `|${headers.map((_, i) => (i === 0 ? '---' : '---:')).join('|')}|`,
    ...rows.map(row => `| ${row.map(v => v ?? '-').join(' | ')} |`),
  ].join('\n');
}

// Delta against a previous result, per page: "p95 120 -> 80 (-33%)"
function comparison(previous, current) {
  const delta = (before, after) => {
    if (before == null || after == null) return '-';
    const pct = before === 0 ? 0 : Math.round(((after - before) / before) * 100);
    return `${before} → ${after} (${pct > 0 ? '+' : ''}${pct}%)`;
  };
  return current.pages.map(row => {
    const old = previous.pages.find(p => p.page === row.page) ?? {};
    return {
      page: row.page,
      avgMs: delta(old.avgMs, row.avgMs),
      p50Ms: delta(old.p50Ms, row.p50Ms),
      p95Ms: delta(old.p95Ms, row.p95Ms),
      failed: delta(old.failed, row.failed),
    };
  });
}

console.log(`Run ${runId}`);
console.log(`Load testing ${config.base}: ${config.users} users, ${config.durationSec}s, ${config.rampSec}s ramp-up, seed ${config.seed}`);
const progress = setInterval(() => {
  const done = [...stats.values()].reduce(
    (n, s) => n + s.times.length + Object.values(s.errors).reduce((a, b) => a + b, 0), 0);
  const elapsed = (performance.now() - start) / 1000;
  process.stdout.write(`\r${elapsed.toFixed(0)}s  ${done} requests  ${(done / elapsed).toFixed(1)} req/s   `);
}, 1000);

await Promise.all(Array.from({ length: config.users }, (_, i) => virtualUser(i)));
clearInterval(progress);

const finishedAt = new Date();
const elapsedSec = (performance.now() - start) / 1000;
const all = [...stats.values()];
const merge = key => all.reduce((acc, s) => {
  for (const [k, v] of Object.entries(s[key])) acc[k] = (acc[k] ?? 0) + v;
  return acc;
}, {});
const statuses = merge('statuses');
const errors = merge('errors');
const pages = paths.map(p => summarise(p, stats.get(p).times, stats.get(p).statuses, stats.get(p).errors));
pages.push(summarise('TOTAL', all.flatMap(s => s.times), statuses, errors));

const result = {
  runId,
  label,
  userAgent,
  startedAt: startedAt.toISOString(),
  finishedAt: finishedAt.toISOString(),
  config,
  throughputRps: Number((pages.at(-1).requests / elapsedSec).toFixed(1)),
  statuses,
  errors,
  pages,
};

const previous = args.compare ? JSON.parse(await readFile(args.compare, 'utf8')) : null;
const compared = previous ? comparison(previous, result) : null;

console.log('\n');
console.table(pages);
console.log(`Throughput: ${result.throughputRps} req/s over ${elapsedSec.toFixed(1)}s`);
console.log('Status codes:', statuses);
if (Object.keys(errors).length) console.log('Errors:', errors);
if (compared) {
  console.log(`\nCompared with ${previous.runId}:`);
  console.table(compared);
}

const md = [
  `# Load test ${runId}`,
  '',
  `- Site: ${config.base}`,
  `- Window (UTC): ${result.startedAt} to ${result.finishedAt}`,
  `- Users: ${config.users}, duration: ${config.durationSec}s, ramp-up: ${config.rampSec}s, seed: ${config.seed}`,
  `- User-Agent: \`${userAgent}\``,
  `- Throughput: ${result.throughputRps} req/s`,
  `- Status codes: ${JSON.stringify(statuses)}`,
  ...(Object.keys(errors).length ? [`- Errors: ${JSON.stringify(errors)}`] : []),
  '',
  markdownTable(columns, pages.map(p => columns.map(c => p[c]))),
  ...(compared
    ? ['', `## Compared with ${previous.runId}`, '',
       markdownTable(['page', 'avgMs', 'p50Ms', 'p95Ms', 'failed'], compared.map(c => [c.page, c.avgMs, c.p50Ms, c.p95Ms, c.failed]))]
    : []),
  '',
].join('\n');

await mkdir(resultsDir, { recursive: true });
await writeFile(join(resultsDir, `${runId}.json`), JSON.stringify(result, null, 2));
await writeFile(join(resultsDir, `${runId}.md`), md);
console.log(`\nResults: ${join(resultsDir, runId)}.json / .md`);
