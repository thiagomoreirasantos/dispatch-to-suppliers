import { Writer } from 'k6/x/kafka';
import { b64encode } from 'k6/encoding';

const brokers = (__ENV.KAFKA_BROKERS || '127.0.0.1:29092').split(',');
const topic = __ENV.KAFKA_TOPIC || 'product-dispatches';
const targetEndpoint = __ENV.TARGET_ENDPOINT || 'http://target-endpoint:7070/api/receiving';
const requiredAcks = Number.isNaN(parseInt(__ENV.KAFKA_ACKS ?? '1', 10))
  ? 1
  : parseInt(__ENV.KAFKA_ACKS ?? '1', 10);
const batchSize = Number.isNaN(parseInt(__ENV.BATCH_SIZE ?? '1', 10))
  ? 1
  : parseInt(__ENV.BATCH_SIZE ?? '1', 10);

export const options = {
  scenarios: {
    dispatches: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE || 50), // mensagens/segundo (carga mínima)
      timeUnit: '1s',
      duration: __ENV.DURATION || '5m',
      preAllocatedVUs: Number(__ENV.PRE_ALLOCATED_VUS || 10),
      maxVUs: Number(__ENV.MAX_VUS || 10),
      exec: 'produceDispatch',
    },
  },
};

const writer = new Writer({
  brokers,
  topic,
  requiredAcks,
});

function buildPayload() {
  const id = `${__VU}-${__ITER}-${Date.now()}`;
  return JSON.stringify({
    supplierId: `supplier-${__VU}`,
    productCode: `SKU-${id}`,
    quantity: Math.floor(Math.random() * 5) + 1,
    targetEndpoint,
    notes: 'k6-kafka-load',
    dispatchId: id,
  });
}

export function produceDispatch() {
  const messages = Array.from({ length: batchSize }).map(() => {
    const key = b64encode(`${__VU}-${Date.now()}`);
    const value = b64encode(buildPayload());
    return { key, value };
  });

  writer.produce({
    messages,
  });
}

// Mantém compatibilidade com k6 cli flags `--vus/--duration`
export default function () {
  produceDispatch();
}

export function teardown() {
  writer.close();
}
