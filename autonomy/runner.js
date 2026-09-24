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
    modelMinIntervalMs: n('CITY_AUTONOMY_MODEL_INTERVAL_MINUTES', 60, 5, 1440) * 60 * 1000,
    maxModelCallsPerDay: n('CITY_AUTONOMY_MAX_MODEL_CALLS_PER_DAY', 24, 1, 200),
    maxMutationsPerDay: n('CITY_AUTONOMY_MAX_MUTATIONS_PER_DAY', 6, 1, 50),
    minMinutesBetweenMutations: n('CITY_AUTONOMY_MUTATION_COOLDOWN_MINUTES', 120, 15, 1440),
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
let lastMutationAt = 0;
let lastHumanSignature = '';
let counters = loadCounters();

function loadCounters() {
  const today = localDay();
  try {
    const saved = JSON.parse(fs.readFileSync(COUNTERS_PATH, 'utf8'));
    if (saved.day === today) {
      lastMutationAt = Number(saved.lastMutationAt || 0);
      return { day: today, modelCalls: Number(saved.modelCalls || 0), mutations: Number(saved.mutations || 0) };
    }
  } catch (_) { /* first run */ }
  return { day: today, modelCalls: 0, mutations: 0 };
}
function persistCounters() {
  ensureRuntime();
  try {
    fs.writeFileSync(COUNTERS_PATH + '.tmp', JSON.stringify({ ...counters, lastMutationAt }, null, 2));
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
  const [zones, facilities, growables, roads] = await Promise.all([
    get('/state/zones'), get('/state/facilities?limit=500'), get('/state/growables?limit=500'),
    get('/state/networks?limit=5000&service=Road')
  ]);
  return {
    ...base,
    zones: (zones.zones || []).map(x => ({ zone: x.zone, cells: x.cells, hectares: x.areaHectares })),
    facilities: (facilities.facilities || []).slice(0, 80).map(f => ({ id: f.id, prefab: f.prefab, service: f.service, active: f.active, problems: f.problems, position: f.position, width: f.width, length: f.length })),
    growables: (growables.growables || []).slice(0, 250).map(g => ({ id: g.id, service: g.service, level: g.level, problems: g.problems, abandoned: g.abandoned, position: g.position, width: g.width, length: g.length })),
    roadSegments: (roads.segments || []).map(r => ({ start: r.start, end: r.end }))
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

function validateAction(action, s, daily) {
  if (!action || typeof action !== 'object') return '动作格式无效。';
  if (action.type === 'setSimulationSpeed') {
    return action.speed === 3 ? '' : '只允许维持 3 倍模拟速度。';
  }
  if (['setZone', 'setTaxRate'].includes(action.type)) {
    if (daily.mutations >= config.maxMutationsPerDay) return '已达到每日自动变更上限。';
    if (Date.now() - lastMutationAt < config.minMinutesBetweenMutations * 60 * 1000) return '尚未达到两次自动变更之间的冷却时间。';
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
    if (nearestRoadDistance(action.center, s.roadSegments) > 60) return '候选分区中心离可达道路超过 60 米。';
    const nearbyDevelopment = (s.growables || []).some(g => g.position && Math.hypot(g.position.x - action.center.x, g.position.z - action.center.z) <= 160);
    if (!nearbyDevelopment) return '候选地不在已有社区附近，不自动向外跳跃扩张。';
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
    roadAccessHint: 'The local validator requires proposed zoning to be within 60 m of a road and within 160 m of existing development.'
  };
  return `You supervise a live Cities: Skylines 1 city. Inspect only this compact JSON snapshot and return one strict JSON object matching the provided output schema. Do not call tools, do not execute commands, and do not invent game facts.\n\nYour job: keep the city stable and grow it cautiously. Prefer no action when evidence is weak. You may propose at most ONE action per cycle.\n\nAllowed proposals only: (1) setZone with one of ResidentialLow, CommercialLow, Industrial, Office and a small radius; zone only where demand is high and existing development/road access support it; (2) setTaxRate only to lower an existing too-high tax to 8 or 9; (3) setSimulationSpeed speed 3. Never propose bulldozing, moving buildings, changing roads, placing service buildings, deleting entities, pausing the game, or changing utilities; those require human review. Do not treat LandfillFull by itself as an emergency when there are no building Garbage problems and an active Combustion Plant exists. Never propose more residential zoning merely because demand is high if the current city has unresolved critical-service issues or finances are distressed. If a substantial service-capacity expansion, demolition, new road layout, or financial commitment is needed, set needsHuman=true and explain why; the controller will decide whether the issue warrants pausing. Every action object must include exactly all schema fields: type, zone, center, radius, service, rate, speed. Set unused values to null (and center=null when not zoning).\n\nCurrent snapshot:\n${JSON.stringify(compact)}`;
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
  if (counters.day !== today) { counters = { day: today, modelCalls: 0, mutations: 0 }; lastMutationAt = 0; persistCounters(); }
}
async function execute(action, s) {
  if (action.type === 'setSimulationSpeed') return post('/commands/set-simulation-speed', { paused: false, speed: 3 });
  if (action.type === 'setTaxRate') return post('/commands/set-tax-rate', { service: action.service, rate: Number(action.rate) });
  if (action.type === 'setZone') {
    await post('/commands/set-zone', { zone: action.zone, preserveOccupied: true, center: action.center, radius: Number(action.radius), dryRun: true });
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
        requestHuman(`模型建议未通过本地安全校验：${invalid}`, s, false);
      } else {
        const result = await execute(action, s);
        log('action-applied', { action, result });
        if (!['setSimulationSpeed'].includes(action.type)) { counters.mutations += 1; lastMutationAt = Date.now(); persistCounters(); }
        if (action.type === 'setZone' || action.type === 'setTaxRate') {
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
    else if (plan.actions.length && validateAction(plan.actions[0], s, counters)) status('needs-human', { reason: 'Model action rejected by local safety policy.', gameTime: s.summary.gameTime });
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

module.exports = { evaluateMajor, nearestRoadDistance, readConfig, validateAction };
