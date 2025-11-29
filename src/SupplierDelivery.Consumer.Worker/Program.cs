using Serilog;
using SupplierDelivery.Consumer.Worker.Options;
using SupplierDelivery.Consumer.Worker.Services;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext())
    .ConfigureServices((context, services) =>
    {
        services.Configure<KafkaConsumerOptions>(
            context.Configuration.GetSection(KafkaConsumerOptions.SectionName));

        services.AddHttpClient<RestProductDispatchProcessor>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IProductDispatchProcessor, RestProductDispatchProcessor>();
        services.AddHostedService<ProductDispatchConsumerService>();
    })
    .Build();

await host.RunAsync();
