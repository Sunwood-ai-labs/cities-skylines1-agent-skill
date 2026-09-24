'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

test('migrates old history, drops expired data, and stores compact bounded samples', async () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'city-dashboard-test-'));
  const file = path.join(directory, 'history.json');
  const now = Date.now();
  const snapshot = at => ({ version: 2, at: new Date(at).toISOString(), summary: { gameTime: '2027-01-01', citizens: { count: 2330 }, demand: { residential: 10, commercial: 5, workplace: 0 } }, economy: { balance: 100000, balanceDelta: 100, income: 4000, expenses: 2000, net: 2000 }, problems: { total: 0 }, zones: { zones: [] }, facilities: { total: 1 }, growables: { total: 215, countsByService: { Residential: 155, Commercial: 36, Industrial: 23, Office: 1 } }, taxes: [] });
  fs.writeFileSync(file, JSON.stringify([snapshot(now - 5 * 3600000), snapshot(now - 60000)]));
  const port = 40000 + Math.floor(Math.random() * 20000);
  const child = spawn(process.execPath, [path.join(__dirname, 'server.js'), '--relay'], { env: { ...process.env, CITY_PUSH_TOKEN: 'test-secret', CITY_DASHBOARD_PORT: String(port), CITY_DASHBOARD_DATA: file }, stdio: 'ignore' });
  try {
    let response;
    for (let i = 0; i < 50; i++) {
      try { response = await fetch(`http://127.0.0.1:${port}/api/snapshot`); break; }
      catch (_) { await new Promise(resolve => setTimeout(resolve, 50)); }
    }
    assert.ok(response, 'relay should start');
    let data = await response.json();
    assert.equal(data.history.length, 1);
    assert.equal(data.latest.summary.citizens.count, 2330);
    const pushed = await fetch(`http://127.0.0.1:${port}/api/push`, { method: 'POST', headers: { Authorization: 'Bearer test-secret', 'Content-Type': 'application/json' }, body: JSON.stringify(snapshot(Date.now())) });
    assert.equal(pushed.status, 204);
    data = JSON.parse(fs.readFileSync(file, 'utf8'));
    assert.equal(data.version, 2);
    assert.equal(data.samples.length, 2);
    assert.equal(data.samples[0].income, 4000);
    assert.equal(data.samples[0].residentialBuildings, 155);
    assert.equal(data.samples[0].commercialBuildings, 36);
    assert.equal(data.samples[0].industrialBuildings, 23);
    assert.ok(!data.samples[0].summary, 'history sample must not duplicate full snapshot');
    assert.ok(fs.statSync(file).size < 512 * 1024);
  } finally {
    child.kill();
    fs.rmSync(directory, { recursive: true, force: true });
  }
});
