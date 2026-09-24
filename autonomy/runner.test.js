'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { deriveZoningCandidates, evaluateMajor, nearestRoadDistance, validateAction } = require('./runner');

function sample() {
  return {
    summary: { citizens: { count: 1000 }, demand: { residential: 60, commercial: 20, workplace: 40 } },
    problems: { countsByProblem: { LandfillFull: 1 } }, finance: { balance: 200000 },
    taxes: [{ service: 'Residential', rate: 9 }], zones: [{ zone: 'ResidentialLow', cells: 100 }],
    roadSegments: [{ start: { x: 0, z: 0 }, end: { x: 100, z: 0 } }],
    growables: [{ position: { x: 30, z: 30 } }]
  };
}

test('does not classify a full landfill as a city emergency by itself', () => {
  assert.equal(evaluateMajor(sample()), '');
});

test('escalates widespread service failures and dangerously low funds', () => {
  const city = sample(); city.problems.countsByProblem.Garbage = 6;
  assert.match(evaluateMajor(city), /关键服务故障/);
  const poor = sample(); poor.finance.balance = 1000;
  assert.match(evaluateMajor(poor), /资金余额/);
});

test('measures road distance to segment, not only endpoints', () => {
  assert.equal(nearestRoadDistance({ x: 50, z: 10 }, sample().roadSegments), 10);
});

test('derives sparse frontage points only beside roads near existing development', () => {
  const city = sample();
  const points = deriveZoningCandidates(city);
  assert.ok(points.length > 0);
  assert.ok(points.length <= 16);
  assert.ok(points.every(point => nearestRoadDistance(point, city.roadSegments) <= 30));
  city.growables = [];
  assert.deepEqual(deriveZoningCandidates(city), []);
});

test('only approves small, demanded, connected zoning actions', () => {
  const city = sample();
  const daily = { mutations: 0 };
  const action = { type: 'setZone', zone: 'ResidentialLow', center: { x: 50, z: 20 }, radius: 16 };
  assert.equal(validateAction(action, city, daily), '');
  assert.match(validateAction({ ...action, radius: 80 }, city, daily), /半径/);
  assert.match(validateAction({ ...action, center: { x: 500, z: 500 } }, city, daily), /道路/);
  assert.match(validateAction(action, city, { mutations: 6 }), /每日自动变更上限/);
});

test('tax automation can only lower a high-tax service with an active alert', () => {
  const city = sample(); city.problems.countsByProblem.TaxesTooHigh = 1;
  city.taxes = [{ service: 'Residential', rate: 12 }];
  assert.equal(validateAction({ type: 'setTaxRate', service: 'Residential', rate: 9 }, city, { mutations: 0 }), '');
  assert.match(validateAction({ type: 'setTaxRate', service: 'Residential', rate: 8 }, sample(), { mutations: 0 }), /高税告警/);
});
