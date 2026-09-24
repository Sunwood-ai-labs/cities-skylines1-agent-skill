'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { deriveResidentialHoleCandidates, deriveZoningCandidates, describeZoningCandidate, evaluateMajor, nearestRoadDistance, setZonePreflightHasChanges, validateAction } = require('./runner');

function sample() {
  return {
    summary: { citizens: { count: 1000 }, demand: { residential: 60, commercial: 20, workplace: 40 } },
    problems: { countsByProblem: { LandfillFull: 1 } }, finance: { balance: 200000 },
    taxes: [{ service: 'Residential', rate: 9 }], zones: [{ zone: 'ResidentialLow', cells: 100 }],
    roadSegments: [{ start: { x: 0, z: 0 }, end: { x: 100, z: 0 } }],
    growables: [{ service: 'Residential', position: { x: 30, z: 30 } }]
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

test('uses only real residential hole candidates and fails closed on old bridge preflight', () => {
  const city = sample();
  city.residentialHoleCandidates = [
    { blockId: 1, position: { x: 10, z: 20 }, unzonedCells: 8 },
    { blockId: 2, position: { x: 90, z: 10 }, unzonedCells: 2 }
  ];
  assert.deepEqual(deriveResidentialHoleCandidates(city), [{ x: 10, z: 20 }]);
  assert.equal(setZonePreflightHasChanges({ matchingBlocks: 1, skippedOccupiedBlocks: 1 }), false);
  assert.equal(setZonePreflightHasChanges({ eligibleCells: 0 }), false);
  assert.equal(setZonePreflightHasChanges({ eligibleCells: 6 }), true);
});

test('only approves small, demanded, connected zoning actions', () => {
  const city = sample();
  const daily = { mutations: 0 };
  const action = { type: 'setZone', zone: 'ResidentialLow', center: deriveZoningCandidates(city)[0], radius: 16 };
  assert.equal(validateAction(action, city, daily), '');
  assert.match(validateAction({ ...action, radius: 80 }, city, daily), /半径/);
  assert.match(validateAction({ ...action, center: { x: 500, z: 500 } }, city, daily), /候选点/);
  assert.match(validateAction(action, city, { mutations: 6 }), /每日自动变更上限/);
});

test('allows consecutive validated city changes without a time cooldown', () => {
  const city = sample();
  const action = { type: 'setZone', zone: 'ResidentialLow', center: deriveZoningCandidates(city)[0], radius: 16 };
  assert.equal(validateAction(action, city, { mutations: 0 }), '');
  assert.equal(validateAction(action, city, { mutations: 1 }), '');
});

test('rejects housing candidates next to pollution sources', () => {
  const city = sample();
  const center = deriveZoningCandidates(city)[0];
  city.facilities = [{ service: 'Garbage', prefab: 'Landfill Site', position: { x: center.x + 10, z: center.z } }];
  const described = describeZoningCandidate(center, city);
  assert.equal(described.nearbyHomes, 1);
  assert.ok(described.pollutionBufferMeters < 120);
  assert.match(validateAction({ type: 'setZone', zone: 'ResidentialLow', center, radius: 16 }, city, { mutations: 0 }), /污染源/);
});

test('tax automation can only lower a high-tax service with an active alert', () => {
  const city = sample(); city.problems.countsByProblem.TaxesTooHigh = 1;
  city.taxes = [{ service: 'Residential', rate: 12 }];
  assert.equal(validateAction({ type: 'setTaxRate', service: 'Residential', rate: 9 }, city, { mutations: 0 }), '');
  assert.match(validateAction({ type: 'setTaxRate', service: 'Residential', rate: 8 }, sample(), { mutations: 0 }), /高税告警/);
});
