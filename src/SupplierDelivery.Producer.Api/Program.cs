using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SupplierDelivery.Producer.Api.Contracts;
using SupplierDelivery.Producer.Api.Options;
using SupplierDelivery.Producer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.Configure<KafkaProducerOptions>(
    builder.Configuration.GetSection(KafkaProducerOptions.SectionName));
builder.Services.AddSingleton<IProductDispatchProducer, KafkaProductDispatchProducer>();
builder.Services.AddScoped<DispatchApplicationService>();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/dispatches", async (
    [FromBody] CreateDispatchRequest request,
    DispatchApplicationService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var message = await service.DispatchAsync(request, cancellationToken);
        return Results.Accepted($"/dispatches/{message.DispatchId}", message);
    }
    catch (ValidationException validationException)
    {
        return Results.BadRequest(new { error = validationException.Message });
    }
    catch (Exception exception)
    {
        Log.Error(exception, "Failed to produce dispatch for supplier {SupplierId}", request.SupplierId);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.WithName("CreateDispatch")
.WithSummary("Cria uma requisição de envio e publica no Kafka")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

app.Run();
