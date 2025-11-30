# SupplierDelivery

Solucao simples de produtor e consumidor Kafka em .NET 10 usando dominio de "envio de produtos para fornecedores".

## Estrutura
- `src/SupplierDelivery.Domain`: contratos e entidades (DDD), sem dependencias de infra.
- `src/SupplierDelivery.Producer.Api`: API minimalista para receber payloads e publicar no Kafka.
- `src/SupplierDelivery.Consumer.Worker`: worker que consome do Kafka e encaminha via HTTP.
- `Dockerfile.producer`, `Dockerfile.consumer`, `docker-compose.yml`: containers das apps e Kafka.

## Pre-requisitos
- Docker Desktop
- Opcional: .NET 10 SDK para builds locais

## Executar com Docker Compose
```bash
docker compose up --build
```
Isso sobe Kafka, API em `http://localhost:8080` e worker consumindo o topico `product-dispatches`.

## Enviar um dispatch
```bash
curl -X POST http://localhost:8080/dispatches \
  -H "Content-Type: application/json" \
  -d '{
    "supplierId": "acme",
    "productCode": "SKU-123",
    "quantity": 5,
    "targetEndpoint": "http://example.com/api/receiving",
    "notes": "lote teste"
  }'
```
A mensagem sera publicada no Kafka e o worker tentara POSTar o mesmo payload no `targetEndpoint`.

Para testar localmente, voce pode usar a API falsa em `src/SupplierDelivery.TargetEndpoint.Api`, que expoe `POST /api/receiving` e apenas responde com 202 (sem corpo). Suba com:
```bash
dotnet run --project src/SupplierDelivery.TargetEndpoint.Api/SupplierDelivery.TargetEndpoint.Api.csproj --urls http://localhost:7070
```
Depois envie o `targetEndpoint` como `http://localhost:7070/api/receiving`.

## Teste de carga (k6)
- Pre-requisito: k6 instalado (docs: https://grafana.com/docs/k6/latest/).
- Suba a stack: `docker compose up --build`.
- Rode o teste (fora do Docker): `BASE_URL=http://localhost:8080 k6 run tests/load/k6/producer-load.js`.
- O script chama `POST /dispatches` e usa `http://target-endpoint:7070/api/receiving` como destino interno; ajuste `BASE_URL` se a API expuser outra porta/host.
- Thresholds do script: `http_req_failed<1%` e p95 de duracao < 800ms; use a saida do k6 para avaliar.

## Configuracao
- Ajuste `appsettings*.json` para endpoints, topico e bootstrap servers.
- Variaveis de ambiente `Kafka__BootstrapServers`, `Kafka__Topic`, `Kafka__ClientId`, `Kafka__GroupId` podem sobrescrever.

## Logs
- Serilog configurado para console com niveis Info/Warn/Error. Use `Serilog:MinimumLevel=Debug` para mais verbosidade em dev.

## Build local
```bash
dotnet build SupplierDelivery.sln -v minimal
```
