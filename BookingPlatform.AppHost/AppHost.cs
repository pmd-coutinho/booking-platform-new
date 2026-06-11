var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var bookingdb = postgres.AddDatabase("bookingdb");

var server = builder.AddProject<Projects.BookingPlatform_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(bookingdb)
    .WaitFor(bookingdb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
