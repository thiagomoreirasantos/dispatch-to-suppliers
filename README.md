# SupplierDelivery

Solução simples de produtor e consumidor Kafka em .NET 10 usando domínio de "envio de produtos para fornecedores".

## Estrutura
- `src/SupplierDelivery.Domain`: contratos e entidades (DDD), sem dependências de infra.
- `src/SupplierDelivery.Producer.Api`: API minimalista para receber payloads e publicar no Kafka.
- `src/SupplierDelivery.Consumer.Worker`: worker que consome do Kafka e encaminha via HTTP.
- `Dockerfile.producer`, `Dockerfile.consumer`, `docker-compose.yml`: contêineres das apps e Kafka.

## Pré-requisitos
- Docker Desktop
- Opcional: .NET 10 SDK para builds locais

## Executar com Docker Compose
```bash
docker compose up --build
```
Isso sobe Kafka, API em `http://localhost:8080` e worker consumindo o tópico `product-dispatches`.

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
A mensagem será publicada no Kafka e o worker tentará POSTar o mesmo payload no `targetEndpoint`.

## Configuração
- Ajuste `appsettings*.json` para endpoints, tópico e bootstrap servers.
- Variáveis de ambiente `Kafka__BootstrapServers`, `Kafka__Topic`, `Kafka__ClientId`, `Kafka__GroupId` podem sobrescrever.

## Logs
- Serilog configurado para console com níveis Info/Warn/Error. Use `Serilog:MinimumLevel=Debug` para mais verbosidade em dev.

## Build local
```bash
dotnet build SupplierDelivery.sln -v minimal
```
