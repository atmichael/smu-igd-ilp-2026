using ILP.Console.Config;
using LMKit.Data;
using LMKit.Extraction.Ocr;
using LMKit.Global;
using LMKit.Model;
using LMKit.TextGeneration;
using LMKit.TextGeneration.Chat;
using SkiaSharp;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Rendering.Skia;

internal class Program
{
    private static readonly string ModelName = "google/gemini-2.5-flash";


    private static async Task Main(string[] args)
    {

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
            string extractedText = await ExtractTextNativelyFromPdfAsync(pdfPath);

            Console.WriteLine("\n--- Extracted Text from PDF Natively ---");
            Console.WriteLine(extractedText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }

    }

    /// <summary>
    /// Encodes a PDF file to Base64 and transcribes its contents using OpenRouter.
    /// </summary>
    public static async Task<string> ExtractTextNativelyFromPdfAsync(string pdfPath)
    {
        // 1. Read entire PDF file bytes and encode to a PDF Data URI
        byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        string base64Pdf = Convert.ToBase64String(pdfBytes);
        string pdfDataUri = $"data:application/pdf;base64,{base64Pdf}";

        // 2. Build the OpenRouter-specific "file" type payload payload
        var payload = new
        {
            model = ModelName,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Read this complete PDF document and transcribe all of its textual contents accurately layout by layout." },
                        new {
                            type = "file",
                            file = new {
                                filename = Path.GetFileName(pdfPath),
                                file_data = pdfDataUri
                            }
                        }
                    }
                }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);

        // 3. Make HTTP request to OpenRouter API
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KeyConfig.OpenRouterApiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://your-app-domain.com");
            client.DefaultRequestHeaders.Add("X-Title", ".NET 10 Native PDF Reader");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenRouter API Error: {response.StatusCode} - {jsonResponse}");
            }

            Console.WriteLine($"==========================================");
            Console.WriteLine($"OpenRouter API Response: {jsonResponse}");
            Console.WriteLine($"==========================================");

            // 4. Extract and return content response
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                return doc.RootElement
                          .GetProperty("choices")
                          .GetProperty("message")
                          .GetProperty("content")
                          .GetString();
            }
        }
    }

    private static async Task ExtractUsingEmbeddedSLM(string pdfPath)
    {
        var modelPath = @"C:\code\smu-igd-ilp-2026\src\model-providers\Qwen2-VL-2B-Instruct-Q6_K.gguf";
        // 1. Initialize the global runtime first (Best practice for LM-Kit)
        // Ensure CUDA / Vulkan aren't taking priority if you want strict CPU testing
        Runtime.EnableCuda = false;
        Runtime.EnableVulkan = false;
        Runtime.Initialize();

        // 2. Configure the local model using the correct DeviceConfiguration class
        var cpuConfig = new LM.DeviceConfiguration
        {
            // Setting this to 0 prevents VRAM offloading and forces system RAM/CPU execution
            GpuLayerCount = 0
        };

        using (LM model = new LM(modelPath, deviceConfiguration: cpuConfig))
        {
            using (var document = PdfDocument.Open(pdfPath, SkiaRenderingParsingOptions.Instance))
            {
                document.AddSkiaPageFactory();

                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    Console.WriteLine($"\nProcessing Page {p}...");

                    using (SKBitmap bitmap = document.GetPageAsSKBitmap(
                        p, 2.0f, SKColor.Parse("#FFFFFF")))
                    using (var skData = bitmap.Encode(SKEncodedImageFormat.Png, 80))
                    using (MemoryStream ms = new MemoryStream())
                    {
                        skData.SaveTo(ms);
                        byte[] imageBytes = ms.ToArray();

                        // SOLUTION: Avoid ImageData entirely. Initialize an explicit 
                        // data attachment instance directly from the raw byte array buffer.
                        var pageAttachment = new Attachment(imageBytes, $"image_{p:00}.png");

                        // Initialize the general multi-turn vision chat wrapper
                        var chat = new MultiTurnConversation(model);

                        // FIX 2: Set strict System constraints to override alignment false-positives
                        chat.SystemPrompt = "You are a professional, local OCR utility engine. " +
                                            "Your ONLY task is to read the attached document image and write down the text exactly as it appears. " +
                                            "Do not comment, do not interpret, do not judge the content, and do not refuse.";


                        string prompt = "Transcribe all visible text from this page.";
                        var messagePayload = new ChatHistory.Message(prompt, pageAttachment);

                        // 4. Correct signature call: Submit text and attachment together asynchronously
                        var response = await chat.SubmitAsync(messagePayload, CancellationToken.None);

                        Console.WriteLine($"--- Page {p} Result ---");
                        Console.WriteLine(response.Completion);
                    }
                }
            }
        }
    }
}