import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  scenarios: {
    dispatches: {
      executor: 'ramping-arrival-rate',
      startRate: 5,            // requisições/s no início
      timeUnit: '1s',
      preAllocatedVUs: 20,
      maxVUs: 150,
      stages: [
        { target: 50, duration: '2m' },  // sobe até 50 req/s
        { target: 50, duration: '3m' },  // plateau
        { target: 0, duration: '30s' },  // rampa de descida
      ],
      exec: 'createDispatch',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],      // <1% de falha
    http_req_duration: ['p(95)<800'],    // 95% abaixo de 800ms
  },
};

function buildPayload() {
  const id = `${__VU}-${__ITER}-${Date.now()}`;
  return JSON.stringify({
    supplierId: `supplier-${__VU}`,
    productCode: `SKU-${id}`,
    quantity: Math.floor(Math.random() * 5) + 1,
    targetEndpoint: 'http://target-endpoint:7070/api/receiving', // ajuste para seu endpoint alvo
    notes: 'load-test',
  });
}

export function createDispatch() {
  const res = http.post(`${baseUrl}/dispatches`, buildPayload(), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status 202': (r) => r.status === 202,
  });
}
