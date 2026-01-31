using System.Net.Http.Headers;
using System.Text;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using tryAGI.OpenAI; // OpenAI API client used by the provider (from LangChain.Providers.OpenAI)

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 1) Read your OpenRouter key (never hardcode secrets)
        var key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Set OPENROUTER_API_KEY and re-run.");
            Console.ResetColor();
            //Console.ReadLine();
            return 1;
            
        }

        // 2) Create an HttpClient with the OpenRouter Bearer token and base URL
        // OpenRouter is OpenAI-compatible at https://openrouter.ai/api/v1. [1](https://github.com/OpenRouterTeam/openrouter-examples)
        var http = new HttpClient
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/")
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        // 3) Build the lower-level OpenAI client, then the LangChain provider
        var api = new OpenAiClient(http, baseUri: http.BaseAddress); // baseUri required in some versions
        var provider = new OpenAiProvider(api);                       // note: no 'baseUrl' named param here (by design)

        // 4) Create the chat model from the provider (constructor that exists in all current builds)
        var model = new OpenAiLatestFastChatModel(provider);          // official sample shows similar usage [4](https://www.datacamp.com/blog/top-open-source-llms)

        // If you want to pin a specific model (otherwise OpenRouter may use your default):
        // model.Model = "meta-llama/llama-3.1-70b-instruct"; // or any other model id set in OpenRouter


        // if (args.Length == 0)
        // {
        //     await SingleShot(model);
        // }
        // else
        // {
        //     var mode = args[0].Trim().ToLowerInvariant();
        //     if (mode is "chat") await ChatWithMemory(model);
        //     else                await SingleShot(model);
        // }
            await ChatWithMemory(model);

        return 0;
    }

    private static async Task SingleShot(OpenAiLatestFastChatModel model)
    {
        // Minimal one-off call (mirrors official sample’s GenerateAsync usage) [4](https://www.datacamp.com/blog/top-open-source-llms)
        var prompt = "Translate to French: I love programming in C#.";
        var result = await model.GenerateAsync(prompt);

        Console.WriteLine("\n=== Single-Shot Result ===");
        Console.WriteLine(result);
        //Console.ReadLine();
    }

    private static async Task ChatWithMemory(OpenAiLatestFastChatModel model)
    {
        // Simple string-based memory (avoids missing AiPrefix/HumanPrefix)
        var history = new StringBuilder();
        Console.WriteLine("Type 'exit' to quit.");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nYou: ");
            Console.ResetColor();

            var user = Console.ReadLine() ?? string.Empty;
            if (user.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            // Append the new turn and build the full prompt
            history.AppendLine($"Human: {user}");

            var template = @"The following is a friendly conversation between a human and an AI.
The AI is helpful, concise, and remembers context.

{HISTORY}
AI:";
            var prompt = template.Replace("{HISTORY}", history.ToString());

            var ai = await model.GenerateAsync(prompt);
            var aiResponse = ai.ToString().Trim();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Wholestack AI: ");
            Console.ResetColor();
            Console.WriteLine(aiResponse);

            // Save reply into the buffer
            history.AppendLine($"Wholestack Responding : {aiResponse}");
        }
    }
}