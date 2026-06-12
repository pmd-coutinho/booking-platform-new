using Scalar.AspNetCore;
using BookingPlatform.Server;
using BookingPlatform.Server.Modules.Businesses.Domain;
using Marten;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;
using JasperFx.Events;
using JasperFx;
using JasperFx.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisClientBuilder("cache")
    .WithOutputCache();
builder.AddNpgsqlDataSource("bookingdb");
builder.Services.AddBusinessesModule();

// Configure Marten with greenfield optimized settings
builder.Services.AddMarten(m =>
{
    var connectionString = builder.Configuration.GetConnectionString("bookingdb")
        ?? "Host=localhost;Port=5432;Database=bookingdb;Username=postgres;Password=postgres";
    m.Connection(connectionString);
    m.Events.AppendMode = EventAppendMode.Quick;
    m.Events.UseArchivedStreamPartitioning = true;
    m.Events.EnableAdvancedAsyncTracking = true;
    m.Events.EnableEventSkippingInProjectionsOrSubscriptions = true;
    m.Events.UseIdentityMapForAggregates = true;
    m.Events.UseMandatoryStreamTypeDeclaration = true;
    m.Events.MetadataConfig.HeadersEnabled = true;
    m.Schema.For<SlugReservation>().UniqueIndex(x => x.Id);
    m.OpenTelemetry.TrackConnections = builder.Environment.IsDevelopment()
        ? TrackLevel.Verbose
        : TrackLevel.Normal;
    m.OpenTelemetry.TrackEventCounters();
    m.DisableNpgsqlLogging = true;
})
.UseLightweightSessions()
.IntegrateWithWolverine(x =>
{
    x.UseWolverineManagedEventSubscriptionDistribution = true;
});

// Configure Wolverine with greenfield optimized settings
builder.Host.UseWolverine(opts =>
{
    opts.UseRuntimeCompilation();

    opts.Durability.EnableInboxPartitioning = true;
    opts.Durability.InboxStaleTime = TimeSpan.FromMinutes(10);
    opts.Durability.OutboxStaleTime = TimeSpan.FromMinutes(10);
    opts.EnableAutomaticFailureAcks = false;
    opts.UnknownMessageBehavior = UnknownMessageBehavior.DeadLetterQueue;

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
});

// Add Wolverine HTTP endpoints
builder.Services.AddWolverineHttp();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Booking Platform Server")
            .ShowOperationId()
            .SortTagsAlphabetically()
            .SortOperationsByMethod();
    });
}

app.UseOutputCache();

// Map Wolverine HTTP endpoints
app.MapWolverineEndpoints(options =>
{
    options.UseFluentValidationProblemDetailMiddleware();
});

app.MapDefaultEndpoints();

app.UseFileServer();

return await app.RunJasperFxCommands(args);

public partial class Program { }


