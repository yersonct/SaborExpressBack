import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const webDocumentDuration = new Trend('web_document_duration_ms', true);
const webDocumentErrors = new Rate('web_document_error_rate');
const warmupSeconds = positiveInteger(__ENV.WARMUP_SECONDS, 120, 'WARMUP_SECONDS');
const rampSeconds = positiveInteger(__ENV.RAMP_SECONDS, 120, 'RAMP_SECONDS');
const steadySeconds = positiveInteger(__ENV.STEADY_SECONDS, 600, 'STEADY_SECONDS');
const vus = positiveInteger(__ENV.LOAD_VUS, 15, 'LOAD_VUS');
const thinkTimeSeconds = positiveInteger(__ENV.THINK_TIME_SECONDS, 1, 'THINK_TIME_SECONDS');
const webBaseUrl = required(__ENV.WEB_BASE_URL, 'WEB_BASE_URL').replace(/\/$/, '');
const pagePath = __ENV.WEB_PAGE_PATH || '/login';
if (!pagePath.startsWith('/') || /[?#]/.test(pagePath)) throw new Error('WEB_PAGE_PATH debe ser una ruta relativa, sin query ni fragmento.');

export const options = {
  discardResponseBodies: true,
  systemTags: ['status', 'method', 'name', 'check', 'error_code', 'scenario', 'expected_response'],
  summaryTrendStats: ['min', 'avg', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  scenarios: {
    web_warmup: {
      executor: 'constant-vus',
      vus: Math.min(3, vus),
      duration: `${warmupSeconds}s`,
      exec: 'loadWebDocument',
      tags: { phase: 'warmup' },
      gracefulStop: '5s',
    },
    web_load: {
      executor: 'ramping-vus',
      startTime: `${warmupSeconds + 5}s`,
      startVUs: 0,
      stages: [
        { duration: `${rampSeconds}s`, target: vus },
        { duration: `${steadySeconds}s`, target: vus },
        { duration: '30s', target: 0 },
      ],
      exec: 'loadWebDocument',
      tags: { phase: 'measurement' },
      gracefulRampDown: '5s',
      gracefulStop: '5s',
    },
  },
  thresholds: {
    'web_document_error_rate{scenario:web_load}': ['rate<0.01'],
  },
};

export function loadWebDocument() {
  const response = http.get(`${webBaseUrl}${pagePath}`, {
    timeout: '30s',
    tags: { endpoint: 'web-entry-document', name: pagePath, surface: 'web' },
  });
  const statusOk = response.status >= 200 && response.status < 400;
  check(response, { 'web entry document returns 2xx/3xx': () => statusOk }, { endpoint: 'web-entry-document', surface: 'web' });
  webDocumentErrors.add(!statusOk, { endpoint: 'web-entry-document', surface: 'web' });
  webDocumentDuration.add(response.timings.duration, { endpoint: 'web-entry-document', surface: 'web' });
  sleep(thinkTimeSeconds);
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
