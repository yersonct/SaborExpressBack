import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const serverDuration = new Trend('server_duration_ms', true);
const clientDuration = new Trend('api_client_duration_ms', true);
const requestErrorRate = new Rate('api_request_error_rate');
const serverTimingMissingRate = new Rate('server_timing_missing_rate');

const warmupSeconds = positiveInteger(__ENV.WARMUP_SECONDS, 120, 'WARMUP_SECONDS');
const rampSeconds = positiveInteger(__ENV.RAMP_SECONDS, 120, 'RAMP_SECONDS');
const steadySeconds = positiveInteger(__ENV.STEADY_SECONDS, 600, 'STEADY_SECONDS');
const vus = positiveInteger(__ENV.LOAD_VUS, 15, 'LOAD_VUS');
const thinkTimeSeconds = positiveInteger(__ENV.THINK_TIME_SECONDS, 1, 'THINK_TIME_SECONDS');
const maxServerP95Ms = positiveInteger(__ENV.MAX_SERVER_P95_MS, 300, 'MAX_SERVER_P95_MS');
const baseUrl = required(__ENV.BASE_URL, 'BASE_URL').replace(/\/$/, '');

const endpoints = [
  { key: 'health', path: '/health', routeTemplate: '/health', expectedStatus: 200 },
  { key: 'public-branches', path: '/api/Branches/public', routeTemplate: '/api/Branches/public', expectedStatus: 200 },
  { key: 'public-categories', path: '/api/Categories', routeTemplate: '/api/Categories', expectedStatus: 200 },
  { key: 'public-products', path: '/api/Products', routeTemplate: '/api/Products', expectedStatus: 200 },
  ...readExtraEndpoints(),
];

const thresholds = {
  'api_request_error_rate{scenario:api_load}': ['rate<0.01'],
  'server_timing_missing_rate{scenario:api_load}': ['rate==0'],
  [`server_duration_ms{scenario:api_load}`]: [`p(95)<${maxServerP95Ms}`],
};

for (const endpoint of endpoints) {
  thresholds[`server_duration_ms{scenario:api_load,endpoint:${endpoint.key}}`] = [`p(95)<${maxServerP95Ms}`];
  thresholds[`api_request_error_rate{scenario:api_load,endpoint:${endpoint.key}}`] = ['rate<0.01'];
}

export const options = {
  discardResponseBodies: true,
  systemTags: ['status', 'method', 'name', 'check', 'error_code', 'scenario', 'expected_response'],
  summaryTrendStats: ['min', 'avg', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  scenarios: {
    api_warmup: {
      executor: 'constant-vus',
      vus: Math.min(3, vus),
      duration: `${warmupSeconds}s`,
      exec: 'runApiFlow',
      tags: { phase: 'warmup' },
      gracefulStop: '5s',
    },
    api_load: {
      executor: 'ramping-vus',
      startTime: `${warmupSeconds + 5}s`,
      startVUs: 0,
      stages: [
        { duration: `${rampSeconds}s`, target: vus },
        { duration: `${steadySeconds}s`, target: vus },
        { duration: '30s', target: 0 },
      ],
      exec: 'runApiFlow',
      tags: { phase: 'measurement' },
      gracefulRampDown: '5s',
      gracefulStop: '5s',
    },
  },
  thresholds,
};

export function runApiFlow() {
  for (const endpoint of endpoints) {
    const headers = endpoint.tokenEnv
      ? { Authorization: `Bearer ${required(__ENV[endpoint.tokenEnv], endpoint.tokenEnv)}` }
      : {};
    const response = http.get(`${baseUrl}${endpoint.path}`, {
      headers,
      timeout: '30s',
      tags: { endpoint: endpoint.key, name: endpoint.routeTemplate, surface: 'api' },
    });

    const statusOk = response.status === endpoint.expectedStatus;
    const tags = { endpoint: endpoint.key, surface: 'api' };
    check(response, { [`${endpoint.key} returns ${endpoint.expectedStatus}`]: () => statusOk }, tags);
    requestErrorRate.add(!statusOk, tags);
    clientDuration.add(response.timings.duration, tags);

    const duration = readServerTiming(response.headers['Server-Timing']);
    serverTimingMissingRate.add(duration === null, tags);
    if (duration !== null) serverDuration.add(duration, tags);
  }

  sleep(thinkTimeSeconds);
}

function readExtraEndpoints() {
  const raw = __ENV.PERF_EXTRA_READ_ENDPOINTS;
  if (!raw) return [];

  let configured;
  try {
    configured = JSON.parse(raw);
  } catch {
    throw new Error('PERF_EXTRA_READ_ENDPOINTS debe ser un JSON válido.');
  }
  if (!Array.isArray(configured)) throw new Error('PERF_EXTRA_READ_ENDPOINTS debe ser una lista JSON.');

  const keys = new Set(['health', 'public-branches', 'public-categories', 'public-products']);
  return configured.map((endpoint) => {
    const key = String(endpoint.key || '');
    const path = String(endpoint.path || '');
    const routeTemplate = String(endpoint.routeTemplate || '');
    const tokenEnv = endpoint.tokenEnv ? String(endpoint.tokenEnv) : undefined;
    const expectedStatus = Number(endpoint.expectedStatus ?? 200);

    if (!/^[a-z0-9][a-z0-9-]{0,39}$/.test(key) || keys.has(key)) {
      throw new Error(`Clave de endpoint inválida o repetida: ${key || '(vacía)'}`);
    }
    keys.add(key);
    if (!path.startsWith('/api/') || /[?#]/.test(path) || path.includes('..')) {
      throw new Error(`La ruta de ${key} debe ser una ruta API relativa, sin query ni fragmento.`);
    }
    if (!/^\/api\/[A-Za-z0-9_{}./-]+$/.test(routeTemplate) || /(?:^|\/)\d+(?:\/|$)/.test(routeTemplate)) {
      throw new Error(`routeTemplate inválida para ${key}; usa una plantilla estable sin query ni IDs concretos.`);
    }
    if (tokenEnv && !/^[A-Z][A-Z0-9_]{0,63}$/.test(tokenEnv)) {
      throw new Error(`tokenEnv inválida para ${key}; proporciona el nombre de una variable de entorno.`);
    }
    if (!Number.isInteger(expectedStatus) || expectedStatus < 200 || expectedStatus > 399) {
      throw new Error(`expectedStatus inválido para ${key}.`);
    }
    return { key, path, routeTemplate, tokenEnv, expectedStatus };
  });
}

function readServerTiming(header) {
  if (!header) return null;
  const match = String(header).match(/(?:^|,)\s*app;dur=([0-9]+(?:\.[0-9]+)?)/i);
  return match ? Number(match[1]) : null;
}

function required(value, name) {
  if (!value) throw new Error(`Falta configurar ${name}.`);
  return value;
}

function positiveInteger(value, fallback, name) {
  const parsed = Number(value ?? fallback);
  if (!Number.isInteger(parsed) || parsed <= 0) throw new Error(`${name} debe ser un entero positivo.`);
  return parsed;
}
