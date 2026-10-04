using ILP.Server.Config;
using ILP.Shared.Helper;
using ILP.Shared.InfoExtraction.Provider;
using Microsoft.Extensions.Hosting;
using Serilog;

// Create App builder
var builder = Host.CreateApplicationBuilder();

// Initialize OpenRouterConfig config class
OpenRouterConfig.Initialize(builder.Configuration);

// Initialize Logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();


// Logic starts 

var pdfPath = @"C:\code\smu-igd-ilp-2026\docs\sample\invoices\APSIM-001.pdf";

if (args.Length > 0)
{
    pdfPath = args.First();
}

string traceId = Guid.NewGuid().ToString();

try
{
    LogHelper.Info(traceId, "Step 1: Reading and encoding the local PDF file directly...");
    if (!File.Exists(pdfPath))
    {
        LogHelper.Info(traceId, $"File not found at {pdfPath}");
        return;
    }

    LogHelper.Info(traceId, "Step 2: Sending PDF directly to OpenRouter via Gemini 2.5 Flash...");
    string docContent = await EmailInfoExtractionProvider.GetDocumentContent("", pdfPath);

    LogHelper.Info(traceId, "--- Extracted Text from PDF Natively ---");
    LogHelper.Info(traceId, $"Content: {docContent}");
}
catch (Exception ex)
{

    LogHelper.Error(traceId, ex, "Error occurred: {ex.Message}");
}