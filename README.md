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
