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
const interval = Math.max(1, Number(process.env.CITY_PUSH_INTERVAL || 3)) * 1000;
const detailInterval = Math.max(10000, Number(process.env.CITY_DASHBOARD_DETAIL_INTERVAL || 30000));
const port = Number(process.env.CITY_DASHBOARD_PORT || 8766);
const dataPath = process.env.CITY_DASHBOARD_DATA || path.join(__dirname, 'history.json');
const page = fs.readFileSync(path.join(__dirname, 'index.html'), 'utf8');
const RETENTION_MS = 60 * 60 * 1000;
const MAX_SAMPLES = 1200;
const MAX_FILE_BYTES = 512 * 1024;
const AUTONOMY_EVENT_TYPES = new Set(['model-plan', 'action-applied', 'action-skipped', 'action-rejected', 'human-review-required', 'autosave-requested']);
const MAX_AUTONOMY_EVENTS = 20;
const AUTONOMY_RETENTION_MS = 60 * 60 * 1000;
const AUTONOMY_LOG = path.join(__dirname, '..', 'autonomy', 'runtime', 'autonomy.log');
let latest = null;
let samples = [];
let detailCache = null;
let detailsAt = 0;
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
  const finance = item.economy || {};
  const demand = item.summary.demand || {};
  const zoneCount = name => (item.zones?.zones || []).find(x => x.zone === name)?.cells;
  const buildingCount = name => item.growables?.countsByService?.[name];
  return {
    at: item.at, gameTime: item.summary.gameTime, population: item.summary.citizens.count,
    problems: item.problems?.total || 0,
    balance: finiteOrNull(finance.balance), balanceDelta: finiteOrNull(finance.balanceDelta),
    income: finiteOrNull(finance.income), expenses: finiteOrNull(finance.expenses), net: finiteOrNull(finance.net),
    residentialDemand: finiteOrNull(demand.residential), commercialDemand: finiteOrNull(demand.commercial),
    workplaceDemand: finiteOrNull(demand.workplace),
    residentialCells: finiteOrNull(zoneCount('ResidentialLow')),
    commercialCells: finiteOrNull(zoneCount('CommercialLow')),
    industrialCells: finiteOrNull(zoneCount('Industrial')),
    residentialBuildings: finiteOrNull(buildingCount('Residential')),
    commercialBuildings: finiteOrNull(buildingCount('Commercial')),
    industrialBuildings: finiteOrNull(buildingCount('Industrial'))
  };
}

function finiteOrNull(value) { return Number.isFinite(value) ? value : null; }

function actionLabel(action) {
  if (!action) return '城市管理操作';
  if (action.type === 'setZone') {
    const zones = { ResidentialLow: '住宅', CommercialLow: '商业', Industrial: '工业', Office: '办公' };
    return `划分${zones[action.zone] || action.zone}用地`;
  }
  if (action.type === 'setTaxRate') return `调整${action.service || ''}税率至 ${action.rate}%`;
  if (action.type === 'setSimulationSpeed') return `模拟速度设为 ${action.speed} 倍`;
  return '城市管理操作';
}

function autonomyEventText(event) {
  if (event.message === 'model-plan') {
    if (event.needsHuman) return `需要人工决策：${event.humanReason || event.summary || '重大城市问题'}`;
    return event.actions?.length ? `自治计划：${actionLabel(event.actions[0])}` : `自治巡查：${event.summary || '暂不需要操作'}`;
  }
  if (event.message === 'action-applied') {
    const amount = Number(event.result?.changedCells || 0);
    return `${actionLabel(event.action)}${amount ? `（${amount} 格）` : ''}`;
  }
  if (event.message === 'action-skipped') return `操作已跳过：${event.result?.reason || '预检未通过'}`;
  if (event.message === 'action-rejected') return `操作被安全规则拦截：${event.reason || '不符合规则'}`;
  if (event.message === 'human-review-required') return `等待人工决策：${event.reason || '需要人工检查'}`;
  if (event.message === 'autosave-requested') return '已请求保存自动存档';
  return '';
}

function readAutonomyEvents(now = Date.now()) {
  try {
    const content = fs.readFileSync(AUTONOMY_LOG, 'utf8');
    const cutoff = now - AUTONOMY_RETENTION_MS;
    return content.split('\n').filter(Boolean).flatMap(line => {
      try {
        const event = JSON.parse(line);
        const at = Date.parse(event.at);
        if (!AUTONOMY_EVENT_TYPES.has(event.message) || !Number.isFinite(at) || at < cutoff || at > now + 60000) return [];
        const text = autonomyEventText(event).replace(/[\r\n\t]+/g, ' ').trim().slice(0, 240);
        return text ? [{ at: new Date(at).toISOString(), type: event.message, text }] : [];
      } catch (_) { return []; }
    }).slice(-MAX_AUTONOMY_EVENTS);
  } catch (_) { return []; }
}

function sanitizeAutonomyEvents(events, now = Date.now()) {
  const cutoff = now - AUTONOMY_RETENTION_MS;
  if (!Array.isArray(events)) return [];
  return events.filter(event => event && AUTONOMY_EVENT_TYPES.has(event.type) && Number.isFinite(Date.parse(event.at)))
    .map(event => ({ at: new Date(event.at).toISOString(), type: event.type, text: String(event.text || '').replace(/[\r\n\t]+/g, ' ').trim().slice(0, 240) }))
    .filter(event => Number.isFinite(Date.parse(event.at)) && Date.parse(event.at) >= cutoff && Date.parse(event.at) <= now + 60000 && event.text)
    .sort((a, b) => Date.parse(a.at) - Date.parse(b.at)).slice(-MAX_AUTONOMY_EVENTS);
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
  const includeDetails = !detailCache || Date.now() - detailsAt >= detailInterval;
  const [state, growables] = await Promise.all([
    requestJson(base + '/state/dashboard?includeDetails=' + includeDetails),
    includeDetails ? requestJson(base + '/state/growables?limit=0') : Promise.resolve(null)
  ]);
  if (!state.ok || !state.summary || !state.economy?.finance) throw new Error('game dashboard state unavailable');
  if (state.detailsIncluded) {
    if (!growables?.ok || !growables.countsByService) throw new Error('game building-count state unavailable');
    detailCache = { problems: state.problems, zones: state.zones, facilities: state.facilities, growables };
    detailsAt = Date.now();
  }
  return {
    version: 2, at: new Date().toISOString(), summary: state.summary,
    economy: state.economy.finance, taxes: state.economy.aggregateTaxRates,
    problems: { total: detailCache.problems.total, countsByProblem: detailCache.problems.countsByProblem },
    zones: { zonedCells: detailCache.zones.zonedCells, zones: detailCache.zones.zones },
    facilities: { total: detailCache.facilities.total, countsByService: detailCache.facilities.countsByService },
    growables: { total: detailCache.growables.total, countsByService: detailCache.growables.countsByService },
    autonomyEvents: readAutonomyEvents()
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
          const received = JSON.parse(raw);
          const item = { ...received, autonomyEvents: sanitizeAutonomyEvents(received.autonomyEvents) };
          const sample = toSample(item);
          const age = Date.now() - Date.parse(item.at);
          if (item.version !== 2 || !sample || age < -60000 || age > RETENTION_MS) throw new Error('invalid or expired snapshot');
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
