using ILP.Server.Config;
using ILP.Shared.Helper;
using ILP.Shared.InboxMonitoring;
using ILP.Shared.InboxMonitoring.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// Create app builder
var builder = Host.CreateApplicationBuilder();

// Initialize OpenRouterConfig Class
OpenRouterConfig.Initialize(builder.Configuration);
PromptConfig.Initialize(builder.Configuration);

// Initialize logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// Reuse the shared inbox sync pipeline as a one-off batch run
builder.Services.AddInboxSync(builder.Configuration);
using var host = builder.Build();

try
{
    var result = await host.Services.GetRequiredService<IInboxSyncService>().SyncAsync();
    foreach (var content in result.ExtractedContents)
    {
        LogHelper.Info(result.TraceId, $"Response: {content}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
