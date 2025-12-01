# SupplierDelivery

Simple Kafka producer/consumer solution in .NET 10 using the "dispatch products to suppliers" domain.

## Structure
- `src/SupplierDelivery.Domain`: contracts and entities (DDD), no infra dependencies.
- `src/SupplierDelivery.Producer.Api`: minimal API that receives payloads and publishes to Kafka.
- `src/SupplierDelivery.Consumer.Worker`: worker that consumes from Kafka and forwards via HTTP.
- `Dockerfile.producer`, `Dockerfile.consumer`, `docker-compose.yml`: app containers and Kafka.

## Prerequisites
- Docker Desktop
- Optional: .NET 10 SDK for local builds

## Run with Docker Compose
```bash
docker compose up --build
```
This spins up Kafka, the API at `http://localhost:8080`, and the worker consuming the `product-dispatches` topic.

## Send a dispatch
```bash
curl -X POST http://localhost:8080/dispatches \
  -H "Content-Type: application/json" \
  -d '{
    "supplierId": "acme",
    "productCode": "SKU-123",
    "quantity": 5,
    "targetEndpoint": "http://example.com/api/receiving",
    "notes": "test batch"
  }'
```
The message will be published to Kafka and the worker will try to POST the same payload to the `targetEndpoint`.

To test locally you can use the fake API in `src/SupplierDelivery.TargetEndpoint.Api`, which exposes `POST /api/receiving` and simply returns 202 (no body). Start it with:
```bash
dotnet run --project src/SupplierDelivery.TargetEndpoint.Api/SupplierDelivery.TargetEndpoint.Api.csproj --urls http://localhost:7070
```
Then send `targetEndpoint` as `http://localhost:7070/api/receiving`.

## Load test (k6)
- Prerequisite: k6 installed (docs: https://grafana.com/docs/k6/latest/).
- Bring up the stack: `docker compose up --build`.
- Run the test (outside Docker): `BASE_URL=http://localhost:8080 k6 run tests/load/k6/producer-load.js`.
- The script calls `POST /dispatches` and uses `http://target-endpoint:7070/api/receiving` as the internal destination; adjust `BASE_URL` if the API is exposed on a different port/host.
- Script thresholds: `http_req_failed<1%` and p95 duration < 800ms; use the k6 output to evaluate.

## Configuration
- Adjust `appsettings*.json` for endpoints, topic, and bootstrap servers.
- Environment variables `Kafka__BootstrapServers`, `Kafka__Topic`, `Kafka__ClientId`, `Kafka__GroupId` can override.

## Logs
- Serilog configured for console with Info/Warn/Error levels. Use `Serilog:MinimumLevel=Debug` for more verbosity in dev.

## Local build
```bash
dotnet build SupplierDelivery.sln -v minimal
```

## Test results

### Producer load (tests/load/k6/producer-load.js)
- Rate ~40 req/s (13,035 in 5m30s); peak 26 VUs.
- Latency: avg ~30 ms; p90 ~51 ms; p95 ~73 ms; max ~1.57 s; p95 threshold (<800 ms) satisfied.
- Drops: 14 iterations dropped (~0.1%); 100% HTTP 202 responses.
- Interpretation: producer API handles this load comfortably; few VUs are enough for 40 req/s.
![Producer load result](tests/load/k6/img/Screenshot%202025-12-01%20134559.png)

### Consumer load (tests/load/k6/kafka-load.js)
- Rate 40 it/s (12,001 in 5m); no drops, no errors/retries.
- Send latency (`kafka_writer_write_seconds`): avg ~3.4 ms; p90 ~1.5 ms; p95 ~2.0 ms; max ~135 ms; queue/batch wait negligible (p95 ~0.5 ms).
- VUs: capped at 20 and sufficient to sustain 40 it/s.
- Interpretation: Kafka sending is fast and stable at this load; rare tail to 135 ms with low p95.
![Consumer load result](tests/load/k6/img/Screenshot%202025-12-01%20143051.png)

#### Analysis (English)
producer-load.js (HTTP)

Throughput: ~40 req/s (13,035 in 5m30s) with only 26 VUs peak. Latency: mean ~30 ms; p90 ~51 ms; p95 ~73 ms; max ~1.57 s (long tail but rare). p95 <80 ms as per threshold. Drops: 14 dropped iterations (~0.1%). No HTTP errors (100% 202). Interpretation: the producer API responds well at this load; few VUs are enough for 40 req/s.

kafka-load.js (producer Kafka)

Rate: 40 it/s (12,001 in 5m). No drops, no errors/retries. Send latency: `kafka_writer_write_seconds` mean ~3.4 ms; p90 ~1.5 ms; p95 ~2.0 ms; max ~135 ms. Queue/batch negligible (p95 ~0.5 ms). VUs: steady at 20 max, enough for 40 it/s. Interpretation: Kafka send is very fast and stable at this low load. The 135 ms tail is rare; p95 stays low.
