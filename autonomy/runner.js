'use strict';

// Long-lived, local-only supervisory loop. The model returns a constrained plan;
// this process, not the model, validates and executes the small allow-listed API.
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { spawn } = require('node:child_process');

const ROOT = path.resolve(__dirname, '..');
const RUNTIME = path.join(__dirname, 'runtime');
const CONFIG_PATH = path.join(__dirname, 'config.json');
const STOP_PATH = path.join(RUNTIME, 'STOP');
const STATUS_PATH = path.join(RUNTIME, 'status.json');
const REVIEW_PATH = path.join(RUNTIME, 'needs-human.json');
const LOG_PATH = path.join(RUNTIME, 'autonomy.log');
const COUNTERS_PATH = path.join(RUNTIME, 'counters.json');
const SCHEMA_PATH = path.join(__dirname, 'output-schema.json');
function localDay() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

function readConfig() {
  let disk = {};
  try { disk = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf8')); } catch (_) { /* first run */ }
  const n = (key, fallback, min, max) => Math.max(min, Math.min(max, Number(process.env[key] || disk[key] || fallback)));
  return {
    bridgeUrl: String(process.env.CITY_BRIDGE_URL || disk.bridgeUrl || 'http://127.0.0.1:32123').replace(/\/$/, ''),
    mode: String(process.env.CITY_AUTONOMY_MODE || process.argv.find(x => x.startsWith('--mode='))?.slice(7) || disk.mode || 'observe').toLowerCase(),
    codexBin: process.env.CODEX_BIN || process.argv.find(x => x.startsWith('--codex-bin='))?.slice(12) || disk.codexBin || 'codex.exe',
    model: process.env.CITY_AUTONOMY_MODEL || disk.model || '',
    pollMs: n('CITY_AUTONOMY_POLL_SECONDS', 30, 10, 300) * 1000,
    modelMinIntervalMs: n('CITY_AUTONOMY_MODEL_INTERVAL_MINUTES', 60, 1, 1440) * 60 * 1000,
    maxModelCallsPerDay: n('CITY_AUTONOMY_MAX_MODEL_CALLS_PER_DAY', 24, 1, 1000),
    maxMutationsPerDay: n('CITY_AUTONOMY_MAX_MUTATIONS_PER_DAY', 6, 1, 1000),
    minBalance: n('CITY_AUTONOMY_MIN_BALANCE', 30000, 0, 10000000),
    criticalServiceThreshold: n('CITY_AUTONOMY_CRITICAL_PROBLEMS', 5, 1, 100),
    callTimeoutMs: n('CITY_AUTONOMY_MODEL_TIMEOUT_SECONDS', 180, 30, 600) * 1000
  };
}

const config = readConfig();
let stopping = false;
let working = false;
let prevPopulation = null;
let lastModelAt = 0;
let lastHumanSignature = '';
let counters = loadCounters();

function loadCounters() {
  const today = localDay();
  try {
    const saved = JSON.parse(fs.readFileSync(COUNTERS_PATH, 'utf8'));
    if (saved.day === today) {
      return { day: today, modelCalls: Number(saved.modelCalls || 0), mutations: Number(saved.mutations || 0) };
    }
  } catch (_) { /* first run */ }
  return { day: today, modelCalls: 0, mutations: 0 };
}
function persistCounters() {
  ensureRuntime();
  try {
    fs.writeFileSync(COUNTERS_PATH + '.tmp', JSON.stringify(counters, null, 2));
    fs.renameSync(COUNTERS_PATH + '.tmp', COUNTERS_PATH);
  } catch (error) { log('counter-write-failed', { error: error.message }); }
}

function ensureRuntime() { fs.mkdirSync(RUNTIME, { recursive: true }); }
function log(message, extra = {}) {
  ensureRuntime();
  const line = JSON.stringify({ at: new Date().toISOString(), message, ...extra }) + '\n';
  try {
    if (fs.existsSync(LOG_PATH) && fs.statSync(LOG_PATH).size > 256 * 1024) {
      const recent = fs.readFileSync(LOG_PATH, 'utf8').split('\n').filter(Boolean).slice(-1000);
      fs.writeFileSync(LOG_PATH, recent.join('\n') + '\n');
    }
    fs.appendFileSync(LOG_PATH, line);
  } catch (_) { /* monitoring must not fail because logging failed */ }
  process.stdout.write(line);
}
function status(state, extra = {}) {
  ensureRuntime();
  const value = { at: new Date().toISOString(), state, mode: config.mode, ...extra };
  try { fs.writeFileSync(STATUS_PATH + '.tmp', JSON.stringify(value, null, 2)); fs.renameSync(STATUS_PATH + '.tmp', STATUS_PATH); } catch (error) { log('status-write-failed', { error: error.message }); }
}
function requestHuman(reason, snapshot, pause) {
  ensureRuntime();
  const signature = `${reason}|${Boolean(pause)}`;
  if (signature === lastHumanSignature) return;
  lastHumanSignature = signature;
  const payload = { at: new Date().toISOString(), reason, gameTime: snapshot?.summary?.gameTime || null, population: snapshot?.summary?.citizens?.count ?? null, countsByProblem: snapshot?.problems?.countsByProblem || {}, pauseApplied: false };
  try { fs.writeFileSync(REVIEW_PATH + '.tmp', JSON.stringify(payload, null, 2)); fs.renameSync(REVIEW_PATH + '.tmp', REVIEW_PATH); } catch (_) { }
  if (pause && snapshot?.summary?.simulation?.paused !== true) {
    post('/commands/set-simulation-speed', { paused: true, speed: 3 }).then(() => {
      payload.pauseApplied = true;
      try { fs.writeFileSync(REVIEW_PATH, JSON.stringify(payload, null, 2)); } catch (_) { }
    }).catch(error => log('pause-failed', { error: error.message }));
  }
  log('human-review-required', payload);
}

async function get(endpoint) {
  const response = await fetch(config.bridgeUrl + endpoint, { signal: AbortSignal.timeout(10000) });
  const data = await response.json();
  if (!response.ok || data.ok === false) throw new Error(`${endpoint}: ${data.error || response.status}`);
  return data;
}
async function post(endpoint, body) {
  const response = await fetch(config.bridgeUrl + endpoint, {
    method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body),
    signal: AbortSignal.timeout(15000)
  });
  const data = await response.json();
  if (!response.ok || data.ok === false) throw new Error(`${endpoint}: ${data.error || response.status}`);
  return data;
}

async function snapshot() {
  const [summary, problems, economy] = await Promise.all([
    get('/state/summary'), get('/state/problems?limit=200'), get('/state/economy')
  ]);
  return {
    at: new Date().toISOString(), summary,
    problems: { countsByProblem: problems.countsByProblem || {}, total: problems.total || 0, items: (problems.problems || []).slice(0, 40).map(p => ({ id: p.id, type: p.entityType, problems: p.problemNames || p.problems, position: p.position })) },
    finance: economy.finance || {},
    taxes: (economy.aggregateTaxRates || []).map(x => ({ service: x.service, rate: x.rate })),
    zones: [], facilities: [], growables: [], roadSegments: []
  };
}
async function planningSnapshot(base) {
  const [zones, facilities, growables, roads, zoneAnomalies] = await Promise.all([
    get('/state/zones'), get('/state/facilities?limit=500'), get('/state/growables?limit=500'),
    get('/state/networks?limit=5000&service=Road'), get('/state/zone-anomalies?limit=200&includeUnzonedHoles=true')
  ]);
  const residentialHoles = (zoneAnomalies.anomalies || []).filter(x => x.type === 'patchyUnzonedHoles' && x.dominantZone === 'ResidentialLow' && x.unzonedCells >= 4);
  const residentialHoleCandidates = await Promise.all(residentialHoles.map(async hole => {
    try {
      const preview = await post('/commands/set-zone', {
        zone: 'ResidentialLow', preserveOccupied: true, center: { x: hole.position.x, z: hole.position.z }, radius: 16, dryRun: true
      });
      return { blockId: hole.blockId, position: hole.position, unzonedCells: hole.unzonedCells, zonedCells: hole.zonedCells, eligibleCells: Number(preview.eligibleCells) || 0 };
    } catch (_) {
      return { blockId: hole.blockId, position: hole.position, unzonedCells: hole.unzonedCells, zonedCells: hole.zonedCells, eligibleCells: 0 };
    }
  }));
  const roadSegments = (roads.segments || []).map(r => ({ start: r.start, end: r.end, startNodeId: r.startNodeId, endNodeId: r.endNodeId, prefab: r.prefab }));
  const residentialFrontageCandidates = await Promise.all(deriveZoningCandidates({ roadSegments, growables: growables.growables || [] }).map(async point => {
    try {
      const preview = await post('/commands/set-zone', { zone: 'ResidentialLow', preserveOccupied: true, center: point, radius: 16, dryRun: true });
      return { ...point, eligibleCells: Number(preview.eligibleCells) || 0 };
    } catch (_) { return { ...point, eligibleCells: 0 }; }
  }));
  return {
    ...base,
    zones: (zones.zones || []).map(x => ({ zone: x.zone, cells: x.cells, hectares: x.areaHectares })),
    facilities: (facilities.facilities || []).slice(0, 80).map(f => ({ id: f.id, prefab: f.prefab, service: f.service, active: f.active, problems: f.problems, position: f.position, width: f.width, length: f.length })),
    growables: (growables.growables || []).slice(0, 250).map(g => ({ id: g.id, service: g.service, level: g.level, problems: g.problems, abandoned: g.abandoned, position: g.position, width: g.width, length: g.length })),
    roadSegments,
    residentialHoleCandidates,
    residentialFrontageCandidates,
    roadBuildCandidates: deriveRoadBuildCandidates({ summary: base.summary, finance: base.finance, problems: base.problems, roadSegments, facilities: facilities.facilities || [], growables: (growables.growables || []), residentialHoleCandidates })
  };
}

const serviceProblem = /water|sewage|electric|garbage|fire|death|health|crime|heating/i;
function evaluateMajor(s) {
  const counts = s.problems.countsByProblem || {};
  const garbage = Number(counts.Garbage || 0);
  const critical = Object.entries(counts).filter(([name]) => serviceProblem.test(name) && name !== 'LandfillFull').reduce((sum, [, count]) => sum + Number(count || 0), 0);
  const fatal = Number(counts.FatalProblem || 0);
  const abandoned = Number(counts.Abandoned || 0);
  const balance = Number(s.finance.balance);
  const pop = Number(s.summary.citizens?.count);
  if (Number.isFinite(balance) && balance < config.minBalance) return `资金余额 ${balance} 低于自动管理安全线 ${config.minBalance}。`;
  if (Number(s.finance.net) < -10000) return `预算周期净支出已超过 10,000，需人工审查城市财政。`;
  if (Number.isFinite(critical) && critical >= config.criticalServiceThreshold) return `城市存在 ${critical} 个关键服务故障（不含已满但垃圾告警清零的填埋场）。`;
  if (garbage >= config.criticalServiceThreshold) return `有 ${garbage} 栋建筑出现垃圾问题。`;
  if (fatal >= 10 || abandoned >= 10) return `城市出现严重建筑问题：FatalProblem=${fatal}, Abandoned=${abandoned}。`;
  if (prevPopulation && Number.isFinite(pop) && pop < prevPopulation * 0.92) return `人口较最近观测下降超过 8%：${prevPopulation} → ${pop}。`;
  return '';
}

function nearestRoadDistance(point, roads) {
  let nearest = Infinity;
  for (const segment of roads || []) {
    const a = segment.start, b = segment.end;
    if (!a || !b) continue;
    const dx = b.x - a.x, dz = b.z - a.z;
    const denom = dx * dx + dz * dz;
    const t = denom ? Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.z - a.z) * dz) / denom)) : 0;
    nearest = Math.min(nearest, Math.hypot(point.x - (a.x + t * dx), point.z - (a.z + t * dz)));
  }
  return nearest;
}

function deriveZoningCandidates(s) {
  const candidates = [];
  for (const segment of s.roadSegments || []) {
    if (candidates.length >= 16) break;
    const a = segment.start, b = segment.end;
    if (!a || !b) continue;
    const dx = b.x - a.x, dz = b.z - a.z;
    const length = Math.hypot(dx, dz);
    if (length < 35) continue;
    const mid = { x: (a.x + b.x) / 2, z: (a.z + b.z) / 2 };
    if (!(s.growables || []).some(g => g.position && Math.hypot(g.position.x - mid.x, g.position.z - mid.z) <= 160)) continue;
    // Place small sample points beside road frontage, away from building centers.
    const perp = { x: -dz / length, z: dx / length };
    for (const side of [-1, 1]) {
      const point = { x: Math.round(mid.x + perp.x * 24), z: Math.round(mid.z + perp.z * 24) };
      if (nearestRoadDistance(point, [segment]) > 30) continue;
      if ((s.growables || []).some(g => g.position && Math.hypot(g.position.x - point.x, g.position.z - point.z) < 18)) continue;
      if (candidates.some(other => Math.hypot(other.x - point.x, other.z - point.z) < 64)) continue;
      candidates.push(point);
      if (candidates.length >= 16) break;
    }
  }
  return candidates;
}
function deriveResidentialHoleCandidates(s) {
  const holes = (s.residentialHoleCandidates || []).filter(x => x.position && Number(x.unzonedCells) >= 4 && Number(x.eligibleCells) > 0).map(x => ({ x: x.position.x, z: x.position.z, eligibleCells: Number(x.eligibleCells) }));
  const frontage = (s.residentialFrontageCandidates || []).filter(x => Number(x.eligibleCells) > 0).map(x => ({ x: x.x, z: x.z, eligibleCells: Number(x.eligibleCells) }));
  return [...new Map([...holes, ...frontage].map(x => [`${x.x}:${x.z}`, x])).values()];
}

function pointSegmentDistance(point, a, b) {
  const dx = b.x - a.x, dz = b.z - a.z;
  const denominator = dx * dx + dz * dz;
  const t = denominator ? Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.z - a.z) * dz) / denominator)) : 0;
  return Math.hypot(point.x - (a.x + t * dx), point.z - (a.z + t * dz));
}

function orientation(a, b, c) {
  return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
}

function onSegment(a, b, p) {
  return p.x >= Math.min(a.x, b.x) - 1e-8 && p.x <= Math.max(a.x, b.x) + 1e-8 && p.z >= Math.min(a.z, b.z) - 1e-8 && p.z <= Math.max(a.z, b.z) + 1e-8;
}

function segmentDistance(a, b, c, d) {
  const o1 = orientation(a, b, c), o2 = orientation(a, b, d);
  const o3 = orientation(c, d, a), o4 = orientation(c, d, b);
  const crosses = ((o1 > 0 && o2 < 0) || (o1 < 0 && o2 > 0)) && ((o3 > 0 && o4 < 0) || (o3 < 0 && o4 > 0));
  const touches = (Math.abs(o1) < 1e-8 && onSegment(a, b, c)) || (Math.abs(o2) < 1e-8 && onSegment(a, b, d)) || (Math.abs(o3) < 1e-8 && onSegment(c, d, a)) || (Math.abs(o4) < 1e-8 && onSegment(c, d, b));
  if (crosses || touches) return 0;
  return Math.min(pointSegmentDistance(a, c, d), pointSegmentDistance(b, c, d), pointSegmentDistance(c, a, b), pointSegmentDistance(d, a, b));
}

function deriveRoadBuildCandidates(s) {
  if (Number(s.summary?.demand?.residential || 0) < 60 || Number(s.finance?.balance) < 100000 || Number(s.finance?.net) < -3000 || evaluateMajor(s)) return [];
  const roads = s.roadSegments || [];
  const nodes = new Map();
  for (const road of roads) {
    if (!['Basic Road', 'Gravel Road'].includes(road.prefab)) continue;
    for (const [id, point] of [[road.startNodeId, road.start], [road.endNodeId, road.end]]) {
      if (id === undefined || !point || !Number.isFinite(point.x) || !Number.isFinite(point.z)) continue;
      if (!nodes.has(Number(id))) nodes.set(Number(id), { id: Number(id), point });
    }
  }
  const candidates = [];
  for (const hole of s.residentialHoleCandidates || []) {
    if (!hole.position || Number(hole.unzonedCells) < 4 || Number(hole.eligibleCells) > 0) continue;
    if (roads.some(road => road.start && road.end && pointSegmentDistance(hole.position, road.start, road.end) <= 32)) continue;
    const ranked = [...nodes.values()].map(node => ({ ...node, distance: Math.hypot(node.point.x - hole.position.x, node.point.z - hole.position.z) })).sort((a, b) => a.distance - b.distance);
    for (const node of ranked.slice(0, 8)) {
      if (node.distance < 32 || node.distance > 72) continue;
      const ux = (hole.position.x - node.point.x) / node.distance;
      const uz = (hole.position.z - node.point.z) / node.distance;
      const end = { x: Math.round(hole.position.x - 24 * ux), z: Math.round(hole.position.z - 24 * uz) };
      const length = Math.hypot(end.x - node.point.x, end.z - node.point.z);
      if (length < 16 || length > 48) continue;
      const clearance = Math.min(Infinity, ...(s.growables || []).filter(g => g.position).map(g => pointSegmentDistance(g.position, node.point, end)));
      if (clearance < 18) continue;
      const facilityClearance = Math.min(Infinity, ...(s.facilities || []).filter(f => f.position).map(f => pointSegmentDistance(f.position, node.point, end)));
      if (facilityClearance < 24) continue;
      const trimmedStart = { x: node.point.x + ((end.x - node.point.x) / length) * 10, z: node.point.z + ((end.z - node.point.z) / length) * 10 };
      const roadClearance = Math.min(Infinity, ...roads.filter(r => r.start && r.end).map(r => segmentDistance(trimmedStart, end, r.start, r.end)));
      if (roadClearance < 8) continue;
      candidates.push({ blockId: hole.blockId, start: { x: node.point.x, z: node.point.z }, end, roadPrefab: 'Basic Road', length: Number(length.toFixed(1)), buildingClearance: Number(clearance.toFixed(1)), facilityClearance: Number.isFinite(facilityClearance) ? Number(facilityClearance.toFixed(1)) : null });
      break;
    }
  }
  return candidates.slice(0, 8);
}

function describeZoningCandidate(point, s) {
  const distance = item => item?.position ? Math.hypot(item.position.x - point.x, item.position.z - point.z) : Infinity;
  const nearbyHomes = (s.growables || []).filter(g => g.service === 'Residential' && distance(g) <= 120).length;
  const nearbyShops = (s.growables || []).filter(g => g.service === 'Commercial' && distance(g) <= 120).length;
  const nearbyJobs = (s.growables || []).filter(g => ['Industrial', 'Office'].includes(g.service) && distance(g) <= 160).length;
  const pollutionSources = (s.facilities || []).filter(f => f.service === 'Garbage' || f.prefab === 'Coal Power Plant');
  const pollutionBufferMeters = pollutionSources.length ? Math.round(Math.min(...pollutionSources.map(distance))) : null;
  return { ...point, nearbyHomes, nearbyShops, nearbyJobs, pollutionBufferMeters };
}
function setZonePreflightHasChanges(result) {
  return result && Number.isFinite(Number(result.eligibleCells)) && Number(result.eligibleCells) > 0;
}

function validateAction(action, s, daily) {
  if (!action || typeof action !== 'object') return '动作格式无效。';
  if (action.type === 'setSimulationSpeed') {
    return action.speed === 3 ? '' : '只允许维持 3 倍模拟速度。';
  }
  if (['buildRoad', 'setZone', 'setTaxRate'].includes(action.type)) {
    if (daily.mutations >= config.maxMutationsPerDay) return '已达到每日自动变更上限。';
  }
  if (action.type === 'setTaxRate') {
    if (!['Residential', 'Commercial', 'Industrial', 'Office'].includes(action.service)) return '税种不在白名单中。';
    if (![8, 9].includes(Number(action.rate))) return '税率只能轻微调整到 8–9%。';
    const hasAlert = Number(s.problems.countsByProblem.TaxesTooHigh || 0) > 0;
    const current = s.taxes.filter(x => x.service === action.service).reduce((m, x) => Math.max(m, Number(x.rate)), 0);
    return hasAlert && current > Number(action.rate) ? '' : '仅当存在高税告警且降低当前税率时才允许自动改税。';
  }
  if (action.type === 'setZone') {
    if (!['ResidentialLow', 'CommercialLow', 'Industrial', 'Office'].includes(action.zone)) return '分区类型不在白名单中。';
    if (!Number.isFinite(action.center?.x) || !Number.isFinite(action.center?.z) || Math.abs(action.center.x) > 4000 || Math.abs(action.center.z) > 4000) return '分区中心坐标无效。';
    const radius = Number(action.radius);
    if (!Number.isFinite(radius) || radius < 8 || radius > 24) return '每轮分区半径必须在 8–24 米。';
    const demandKey = action.zone === 'ResidentialLow' ? 'residential' : action.zone === 'CommercialLow' ? 'commercial' : 'workplace';
    if (Number(s.summary.demand?.[demandKey] || 0) < 45) return '对应分区需求不足，不自动扩区。';
    const zoneName = action.zone;
    const existing = (s.zones || []).find(z => z.zone === zoneName)?.cells || 0;
    const cap = zoneName === 'ResidentialLow' ? 2500 : zoneName === 'CommercialLow' ? 650 : zoneName === 'Industrial' ? 500 : 256;
    if (existing >= cap) return `${zoneName} 已达到自动扩区上限 ${cap} 个格。`;
    const candidatePool = zoneName === 'ResidentialLow' && Array.isArray(s.residentialHoleCandidates)
      ? deriveResidentialHoleCandidates(s)
      : deriveZoningCandidates(s);
    const candidate = candidatePool.find(p => Math.hypot(p.x - action.center.x, p.z - action.center.z) <= 1);
    if (!candidate) return '分区中心不在本地预先验证的稀疏候选点中。';
    const context = describeZoningCandidate(candidate, s);
    if (zoneName === 'ResidentialLow' && (context.nearbyHomes === 0 || (context.pollutionBufferMeters !== null && context.pollutionBufferMeters < 120))) return '住宅候选点须邻近现有住宅且与垃圾/煤电污染源保持至少 120 米缓冲。';
    if (zoneName === 'CommercialLow' && context.nearbyShops === 0) return '商业候选点须邻近现有商铺。';
    if (['Industrial', 'Office'].includes(zoneName) && context.nearbyJobs === 0) return '就业用地候选点须邻近现有工作岗位区。';
    if (nearestRoadDistance(action.center, s.roadSegments) > 60) return '候选分区中心离可达道路超过 60 米。';
    const nearbyDevelopment = (s.growables || []).some(g => g.position && Math.hypot(g.position.x - action.center.x, g.position.z - action.center.z) <= 160);
    if (!nearbyDevelopment) return '候选地不在已有社区附近，不自动向外跳跃扩张。';
    return '';
  }
  if (action.type === 'buildRoad') {
    if (daily.mutations >= config.maxMutationsPerDay) return '已达到每日自动变更上限。';
    if (Number(s.finance.balance) < 100000 || Number(s.finance.net) < -3000) return '财政未达到自动扩建道路的安全条件。';
    if (action.roadPrefab !== 'Basic Road' || !action.start || !action.end) return '建路类型或端点无效。';
    const candidate = (s.roadBuildCandidates || []).find(p => ['x', 'z'].every(k => Math.abs(p.start[k] - action.start[k]) <= 1 && Math.abs(p.end[k] - action.end[k]) <= 1));
    if (!candidate) return '道路端点不在本地验证的安全候选列表中。';
    const length = Math.hypot(action.end.x - action.start.x, action.end.z - action.start.z);
    if (length < 16 || length > 48 || candidate.buildingClearance < 18) return '道路长度或建筑安全间距不符合限制。';
    return '';
  }
  return `动作 ${String(action.type)} 不在安全白名单中。`;
}

function makePrompt(s) {
  const byService = {};
  for (const building of s.growables || []) byService[building.service] = (byService[building.service] || 0) + 1;
  const compact = {
    at: s.at, summary: s.summary, problems: s.problems, finance: s.finance, taxes: s.taxes, zones: s.zones,
    growableCountsByService: byService,
    growablesWithProblems: (s.growables || []).filter(x => x.problems || x.abandoned).slice(0, 20),
    facilities: s.facilities,
    safeZoningCandidates: deriveResidentialHoleCandidates(s).map(point => ({ ...point, ...describeZoningCandidate(point, s) })),
    roadBuildCandidates: s.roadBuildCandidates || [],
    roadAccessHint: 'Road priority: if roadBuildCandidates contains entries, propose exactly one listed buildRoad action first. These short Basic Road extensions connect at an existing local-road node, stay clear of buildings and other roads, and end near an existing residential hole. Only after no safe road candidates remain may you zone a listed safeZoningCandidates point. These points include live-preflighted residential holes and empty road frontages near existing homes. Never invent road or zone coordinates.'
  };
  return `You supervise a live Cities: Skylines 1 city. Inspect only this compact JSON snapshot and return one strict JSON object matching the provided output schema. Do not call tools, do not execute commands, and do not invent game facts.\n\nYour job: keep the city stable and grow it cautiously. Prefer no action when evidence is weak. You may propose at most ONE action per cycle.\n\nAllowed proposals only: (1) buildRoad using one roadBuildCandidates entry exactly; (2) setZone with one of ResidentialLow, CommercialLow, Industrial, Office and a small radius; for housing, choose a safeZoningCandidates entry exactly; (3) setTaxRate only to lower an existing too-high tax to 8 or 9; (4) setSimulationSpeed speed 3. Road-first rule: when roadBuildCandidates is non-empty, propose exactly one listed buildRoad action before zoning. A buildRoad action must use type=buildRoad, the listed start/end, roadPrefab=Basic Road, and null for unused fields. Never invent endpoints or build multiple segments per cycle. Never propose bulldozing, moving buildings, deleting entities, placing service buildings, pausing the game, or changing utilities. Do not treat LandfillFull by itself as an emergency when there are no building Garbage problems and an active Combustion Plant exists. Do not ask for human review for a single ElectricityNotConnected netNode when no building/growable has an electricity problem; treat a lone orphan terminal as non-actionable background noise. needsHuman=true is reserved for city-wide/multi-building outages, severe population collapse, dangerously low funds, or other consequential decisions (not small uncertainties).\n\nFor growth, a budget balance above 100000 and net budget at least -3000 is financially healthy enough for one small zoning step; do not reject expansion because of a small budget-window deficit alone. Only when roadBuildCandidates is empty, if residential demand is at least 60, ResidentialLow has fewer than 2300 cells, finances meet that healthy threshold, no critical service problems exist, and safeZoningCandidates include a residential candidate (nearbyHomes>0 and pollutionBufferMeters>=120), propose exactly one ResidentialLow zone at that candidate with radius 16. Do not zone multiple areas or add commercial/industrial cells unless their demand is high too. If one of these conditions is not met, hold. The local validator independently enforces demand, area caps, occupied-cell preservation, cell-level zoning preflight, road endpoint allow-listing, building/road clearances, pollution buffer, and action budget. Every action object must include exactly all schema fields: type, zone, center, radius, service, rate, speed, roadPrefab, start, end. Set unused values to null.\n\nCurrent snapshot:\n${JSON.stringify(compact)}`;
}

function codexPlan(s) {
  return new Promise((resolve, reject) => {
    const outputPath = path.join(os.tmpdir(), `cs1-autonomy-${process.pid}-${Date.now()}.json`);
    // Keep the delegated planner isolated from arbitrary user MCP servers and
    // plugin configuration; Codex documents that authentication is retained.
    const args = ['exec', '--ephemeral', '--ignore-user-config', '--sandbox', 'read-only', '--output-schema', SCHEMA_PATH, '--output-last-message', outputPath, '--json'];
    if (config.model) args.push('--model', config.model);
    args.push('-');
    let settled = false;
    const child = spawn(config.codexBin, args, { cwd: ROOT, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
    let stderr = '';
    let stdout = '';
    child.stdout?.on('data', x => { stdout = (stdout + x.toString()).slice(-8000); });
    child.stderr.on('data', x => { stderr = (stderr + x.toString()).slice(-8000); });
    const timeout = setTimeout(() => { child.kill(); finish(new Error('model request timed out')); }, config.callTimeoutMs);
    function finish(error) {
      if (settled) return;
      settled = true; clearTimeout(timeout);
      try {
        if (error) return reject(new Error(`${error.message}${stderr ? ` stderr: ${stderr.slice(-2000)}` : ''}${stdout ? ` stdout: ${stdout.slice(-3000)}` : ''}`));
        resolve(JSON.parse(fs.readFileSync(outputPath, 'utf8')));
      } catch (e) { reject(new Error(`could not parse model plan: ${e.message}${stderr ? `: ${stderr.slice(-2000)}` : ''}`)); }
      finally { try { fs.rmSync(outputPath, { force: true }); } catch (_) { } }
    }
    child.on('error', error => finish(error));
    child.on('close', code => finish(code === 0 ? null : new Error(`codex exited with ${code}`)));
    child.stdin.end(makePrompt(s));
  });
}

function rolloverDay() {
  const today = localDay();
  if (counters.day !== today) { counters = { day: today, modelCalls: 0, mutations: 0 }; persistCounters(); }
}
async function execute(action, s) {
  if (action.type === 'setSimulationSpeed') return post('/commands/set-simulation-speed', { paused: false, speed: 3 });
  if (action.type === 'setTaxRate') return post('/commands/set-tax-rate', { service: action.service, rate: Number(action.rate) });
  if (action.type === 'buildRoad') {
    const body = { roadPrefab: action.roadPrefab, start: action.start, end: action.end, name: 'Agent Residential Access' };
    const preview = await post('/commands/build-network', { ...body, dryRun: true });
    if (!preview.ok) return { ok: true, skipped: true, reason: preview.message || '道路预检未通过；未建路。' };
    return post('/commands/build-network', body);
  }
  if (action.type === 'setZone') {
    const preview = await post('/commands/set-zone', { zone: action.zone, preserveOccupied: true, center: action.center, radius: Number(action.radius), dryRun: true });
    if (!Number.isFinite(Number(preview.eligibleCells))) return { ok: true, skipped: true, reason: 'Bridge lacks cell-level zoning preflight; no change was made.' };
    if (!setZonePreflightHasChanges(preview)) return { ok: true, skipped: true, reason: 'No valid unoccupied cells would change at this location; no change was made.' };
    const result = await post('/commands/set-zone', { zone: action.zone, preserveOccupied: true, center: action.center, radius: Number(action.radius) });
    return result;
  }
  throw new Error('executor received an unsupported action');
}

async function cycle() {
  rolloverDay();
  let s;
  try { s = await snapshot(); }
  catch (error) { status('bridge-unavailable', { error: error.message }); log('snapshot-failed', { error: error.message }); return; }
  const major = evaluateMajor(s);
  if (major) {
    requestHuman(major, s, true);
    status('needs-human', { reason: major, gameTime: s.summary.gameTime });
    return;
  }
  if (s.summary.simulation?.paused) {
    status('game-paused', { reason: 'Simulation is paused; leaving the game untouched pending an explicit resume request.', gameTime: s.summary.gameTime });
    return;
  }
  if (config.mode === 'observe') {
    status('observing', { gameTime: s.summary.gameTime, population: s.summary.citizens?.count, problems: s.problems.total, balance: s.finance.balance });
    return;
  }
  if (counters.modelCalls >= config.maxModelCallsPerDay) {
    const reason = `达到每日模型调用上限 ${config.maxModelCallsPerDay}；为避免无预算运行，自动变更已暂停，监测继续。`;
    requestHuman(reason, s, false); status('model-budget-exhausted', { reason }); return;
  }
  if (Date.now() - lastModelAt < config.modelMinIntervalMs) {
    status('monitoring', { gameTime: s.summary.gameTime, population: s.summary.citizens?.count, problems: s.problems.total, modelCallsToday: counters.modelCalls });
    return;
  }
  working = true;
  status('thinking', { gameTime: s.summary.gameTime, modelCallsToday: counters.modelCalls });
  let rejectedAction = '';
  let skippedAction = '';
  try {
    s = await planningSnapshot(s);
    lastModelAt = Date.now(); counters.modelCalls += 1; persistCounters();
    const plan = await codexPlan(s);
    log('model-plan', { summary: plan.summary, needsHuman: plan.needsHuman, humanReason: plan.humanReason, actions: plan.actions });
    if (plan.needsHuman) requestHuman(plan.humanReason || plan.summary || '模型请求人工决策。', s, false);
    if (config.mode === 'assist' && plan.actions.length && !plan.needsHuman) {
      requestHuman(`辅助模式建议（未执行）：${JSON.stringify(plan.actions[0])}`, s, false);
    }
    if (config.mode === 'auto' && plan.actions.length && !plan.needsHuman) {
      const action = plan.actions[0];
      const invalid = validateAction(action, s, counters);
      if (invalid) {
        rejectedAction = invalid;
        log('action-rejected', { action, reason: invalid });
      } else {
        const result = await execute(action, s);
        if (result.skipped || (action.type === 'setZone' && Number(result.changedCells) < 1)) {
          skippedAction = result.reason || 'No cells changed; skipped without consuming mutation budget.';
          log('action-skipped', { action, result });
        } else {
          log('action-applied', { action, result });
          if (!['setSimulationSpeed'].includes(action.type)) { counters.mutations += 1; persistCounters(); }
        }
        if (!skippedAction && !rejectedAction && ['buildRoad', 'setZone', 'setTaxRate'].includes(action.type)) {
          // Reuse one rolling checkpoint name; an unattended loop must not grow
          // the local Saves directory by generating a new multi-megabyte save.
          const name = 'AgentAutoSave-autonomy-latest';
          try { await post('/commands/save', { name }); log('autosave-requested', { name }); }
          catch (error) { log('autosave-failed', { error: error.message }); }
        }
      }
    }
    if (plan.needsHuman) status('needs-human', { reason: plan.humanReason, gameTime: s.summary.gameTime });
    else if (config.mode === 'assist' && plan.actions.length) status('assist-review', { plan: plan.actions[0], gameTime: s.summary.gameTime });
    else if (rejectedAction || skippedAction) status('monitoring', { gameTime: s.summary.gameTime, population: s.summary.citizens?.count, problems: s.problems.total, modelCallsToday: counters.modelCalls, mutationsToday: counters.mutations, plan: plan.summary, actionNote: rejectedAction || skippedAction });
    else status('monitoring', { gameTime: s.summary.gameTime, population: s.summary.citizens?.count, problems: s.problems.total, modelCallsToday: counters.modelCalls, mutationsToday: counters.mutations, plan: plan.summary });
  } catch (error) {
    log('model-cycle-failed', { error: error.message }); status('model-error', { error: error.message });
  } finally { working = false; prevPopulation = Number(s.summary.citizens?.count) || prevPopulation; }
}

async function main() {
  if (Number(process.versions.node.split('.')[0]) < 18) throw new Error('Node.js 18 or newer is required.');
  ensureRuntime();
  if (!['observe', 'assist', 'auto'].includes(config.mode)) throw new Error('Mode must be observe, assist, or auto.');
  log('autonomy-started', { mode: config.mode, bridgeUrl: config.bridgeUrl, pollMs: config.pollMs, modelIntervalMs: config.modelMinIntervalMs, dailyModelLimit: config.maxModelCallsPerDay, dailyMutationLimit: config.maxMutationsPerDay });
  while (!stopping) {
    if (fs.existsSync(STOP_PATH)) { log('explicit-stop-file-detected'); status('stopped'); break; }
    if (!working) await cycle();
    await new Promise(resolve => setTimeout(resolve, config.pollMs));
  }
}

if (require.main === module) {
  process.on('SIGINT', () => { stopping = true; });
  process.on('SIGTERM', () => { stopping = true; });
  main().catch(error => { log('fatal', { error: error.stack || error.message }); status('fatal', { error: error.message }); process.exitCode = 1; });
}

module.exports = { deriveResidentialHoleCandidates, deriveRoadBuildCandidates, deriveZoningCandidates, describeZoningCandidate, evaluateMajor, nearestRoadDistance, readConfig, setZonePreflightHasChanges, validateAction };
