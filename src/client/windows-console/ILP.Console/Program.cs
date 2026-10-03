using ILP.Server.Config;
using ILP.Shared.InfoExtraction.Provider;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

OpenRouterConfig.Initialize(builder.Configuration);

var pdfPath = @"C:\code\smu-igd-ilp-2026\docs\sample\invoices\APSIM-001.pdf";

if (args.Length > 0)
{
    pdfPath = args.First();
}

try
{
    Console.WriteLine("Step 1: Reading and encoding the local PDF file directly...");
    if (!File.Exists(pdfPath))
    {
        Console.WriteLine($"Error: File not found at {pdfPath}");
        return;
    }

    Console.WriteLine("Step 2: Sending PDF directly to OpenRouter via Gemini 2.5 Flash...");
    string extractedText = await EmailInfoExtractionProvider.GetDocumentHeader("", new List<string>() { pdfPath });

    Console.WriteLine("\n--- Extracted Text from PDF Natively ---");
    Console.WriteLine(extractedText);
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}