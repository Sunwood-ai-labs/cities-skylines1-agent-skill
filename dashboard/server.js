'use strict';

// Dependency-free dashboard: --push reads the local game API and sends snapshots;
// --relay serves the public, read-only page and persists a bounded history.
const http = require('node:http');
const https = require('node:https');
const fs = require('node:fs');
const path = require('node:path');

const relay = process.argv.includes('--relay');
const token = process.env.CITY_PUSH_TOKEN;
const pushUrl = process.env.CITY_PUSH_URL || 'https://city.whodothis.uk/api/push';
const interval = Math.max(15, Number(process.env.CITY_PUSH_INTERVAL || 60)) * 1000;
const port = Number(process.env.CITY_DASHBOARD_PORT || 8766);
const dataPath = process.env.CITY_DASHBOARD_DATA || path.join(__dirname, 'history.json');
const page = fs.readFileSync(path.join(__dirname, 'index.html'), 'utf8');
const RETENTION_MS = 4 * 60 * 60 * 1000;
const MAX_SAMPLES = 240;
const MAX_FILE_BYTES = 256 * 1024;
let latest = null;
let samples = [];
if (relay) {
  try {
    const saved = JSON.parse(fs.readFileSync(dataPath, 'utf8'));
    // Migrate the old array-of-full-snapshots format on first start.
    if (Array.isArray(saved)) {
      latest = saved.at(-1) || null;
      samples = saved.map(toSample).filter(Boolean);
    } else {
      latest = saved.latest || null;
      samples = Array.isArray(saved.samples) ? saved.samples : [];
    }
  } catch (_) { latest = null; samples = []; }
}

function toSample(item) {
  if (!item || !Number.isFinite(Date.parse(item.at)) || !Number.isFinite(item.summary?.citizens?.count)) return null;
  return { at: item.at, gameTime: item.summary.gameTime, population: item.summary.citizens.count, problems: item.problems?.total || 0 };
}

function prune(now = Date.now()) {
  const cutoff = now - RETENTION_MS;
  samples = samples.filter(x => Number.isFinite(Date.parse(x.at)) && Date.parse(x.at) >= cutoff && Date.parse(x.at) <= now + 60000).slice(-MAX_SAMPLES);
  if (latest && (!Number.isFinite(Date.parse(latest.at)) || Date.parse(latest.at) < cutoff)) latest = null;
}

function persist(nextLatest = latest, nextSamples = samples) {
  const temporary = dataPath + '.tmp';
  let output = Buffer.from(JSON.stringify({ version: 2, latest: nextLatest, samples: nextSamples }));
  // Defensive cap if a future bridge response grows unexpectedly.
  while (output.length > MAX_FILE_BYTES && nextSamples.length) {
    nextSamples = nextSamples.slice(1);
    output = Buffer.from(JSON.stringify({ version: 2, latest: nextLatest, samples: nextSamples }));
  }
  if (output.length > MAX_FILE_BYTES) throw new Error('latest snapshot exceeds dashboard file limit');
  fs.mkdirSync(path.dirname(dataPath), { recursive: true });
  fs.writeFileSync(temporary, output);
  fs.renameSync(temporary, dataPath);
  latest = nextLatest;
  samples = nextSamples;
}

if (relay) {
  prune();
  persist();
  setInterval(() => {
    const before = samples.length;
    const hadLatest = !!latest;
    prune();
    if (before !== samples.length || hadLatest !== !!latest) {
      try { persist(); } catch (error) { process.stderr.write(`prune failed: ${error.message}\n`); }
    }
  }, 60000).unref();
}

function requestJson(url, options = {}) {
  return new Promise((resolve, reject) => {
    const target = new URL(url);
    const transport = target.protocol === 'https:' ? https : http;
    const body = options.body ? Buffer.from(JSON.stringify(options.body)) : null;
    const req = transport.request(target, {
      method: body ? 'POST' : 'GET', timeout: 12000,
      headers: body ? { 'Content-Type': 'application/json', 'Content-Length': body.length, Authorization: `Bearer ${token}` } : {}
    }, res => {
      const chunks = [];
      res.on('data', chunk => chunks.push(chunk));
      res.on('end', () => {
        const output = Buffer.concat(chunks).toString('utf8');
        if (res.statusCode < 200 || res.statusCode >= 300) return reject(new Error(`${target.pathname}: HTTP ${res.statusCode} ${output.slice(0, 100)}`));
        try { resolve(output ? JSON.parse(output) : {}); } catch (_) { resolve({}); }
      });
    });
    req.on('timeout', () => req.destroy(new Error('request timed out')));
    req.on('error', reject);
    if (body) req.write(body);
    req.end();
  });
}

async function collect() {
  const base = 'http://127.0.0.1:32123';
  const summary = await requestJson(base + '/state/summary');
  if (!summary.ok) throw new Error('game summary unavailable');
  // Sequential calls avoid piling up work on the game-thread queue.
  const problems = await requestJson(base + '/state/problems?limit=20');
  const zones = await requestJson(base + '/state/zones');
  const facilities = await requestJson(base + '/state/facilities?limit=0');
  const economy = await requestJson(base + '/state/economy');
  return {
    version: 1, at: new Date().toISOString(), summary,
    problems: { total: problems.total, countsByProblem: problems.countsByProblem, items: problems.problems },
    zones: { zonedCells: zones.zonedCells, zones: zones.zones },
    facilities: { total: facilities.total, countsByService: facilities.countsByService },
    taxes: economy.aggregateTaxRates
  };
}

async function pushLoop() {
  if (!token) throw new Error('CITY_PUSH_TOKEN is required');
  for (;;) {
    try {
      const snapshot = await collect();
      await requestJson(pushUrl, { body: snapshot });
      process.stdout.write(`pushed ${snapshot.at} population=${snapshot.summary.citizens.count}\n`);
      if (process.argv.includes('--once')) return;
    } catch (error) { process.stderr.write(`push failed: ${error.message}\n`); }
    await new Promise(resolve => setTimeout(resolve, interval));
  }
}

function send(res, status, body, type) {
  res.writeHead(status, { 'Content-Type': type, 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
  res.end(body);
}

if (!relay) pushLoop().catch(error => { process.stderr.write(error.message + '\n'); process.exitCode = 1; });
else {
  if (!token) throw new Error('CITY_PUSH_TOKEN is required');
  http.createServer((req, res) => {
    if (req.url === '/api/push' && req.method === 'POST') {
      if (req.headers.authorization !== `Bearer ${token}`) return send(res, 401, 'unauthorized', 'text/plain');
      let raw = '';
      req.on('data', chunk => { raw += chunk; if (raw.length > 262144) req.destroy(); });
      req.on('end', () => {
        try {
          const item = JSON.parse(raw);
          const sample = toSample(item);
          const age = Date.now() - Date.parse(item.at);
          if (item.version !== 1 || !sample || age < -60000 || age > RETENTION_MS) throw new Error('invalid or expired snapshot');
          prune();
          persist(item, samples.concat(sample).slice(-MAX_SAMPLES));
          send(res, 204, '', 'text/plain');
        } catch (error) { send(res, 400, error.message, 'text/plain'); }
      });
      return;
    }
    if (req.url === '/api/snapshot' && req.method === 'GET') return send(res, 200, JSON.stringify({ ok: !!latest, latest, history: samples }), 'application/json; charset=utf-8');
    if ((req.url === '/' || req.url === '/index.html') && req.method === 'GET') return send(res, 200, page, 'text/html; charset=utf-8');
    send(res, 404, 'not found', 'text/plain');
  }).listen(port, '127.0.0.1', () => process.stdout.write(`dashboard relay listening on 127.0.0.1:${port}\n`));
}
