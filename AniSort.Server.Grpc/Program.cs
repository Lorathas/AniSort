using AniSort.Core;
using AniSort.Server.Generators;
using AniSort.Server.Hubs;
using AniSort.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddSingleton<IJobHub, JobHub>();
builder.Services.AddSingleton<ILocalFileHub, LocalFileHub>();
builder.Services.AddSingleton<IScheduledJobHub, ScheduledJobHub>();
Startup.InitializeServices(null, builder.Services);
HubServiceRegistration.RegisterServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<JobService>();
app.MapGrpcService<LocalFileService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

var cts = new CancellationTokenSource();

var jobHub = app.Services.GetService<IJobHub>();
var localFileHub = app.Services.GetService<ILocalFileHub>();
var scheduledJobHub = app.Services.GetService<IScheduledJobHub>();

jobHub!.RunAsync(cts.Token);
localFileHub!.RunAsync(cts.Token);
scheduledJobHub!.RunAsync(cts.Token);

app.Run();

cts.Cancel();