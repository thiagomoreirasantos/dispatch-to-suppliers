using Microsoft.AspNetCore.Mvc;
using SupplierDelivery.Domain.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/receiving", ([FromBody] ProductDispatchMessage dispatch) =>
{
    return Results.Accepted();
})
.WithName("ReceiveDispatch")
.WithSummary("Recebe o dispatch e apenas retorna o status code.")
.Produces(StatusCodes.Status202Accepted);

app.MapGet("/api/receiving", () =>
{
    return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
})
.WithName("ReceivingMethodNotAllowed")
.WithSummary("Retorna 405 para métodos não permitidos, mas evita 404.");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("TargetEndpointHealth");

app.Run();
