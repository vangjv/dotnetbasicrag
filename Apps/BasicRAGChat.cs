/*
 * Streaming RAG Chat Console App (GPT‑4o) – Cached Embeddings + Tool Calling
 * -------------------------------------------------------------------------
 * Adds on‑disk caching so each subject FAQ is embedded **once**.
 *
 * 1.  Place markdown files in a sibling "faqs" folder (sales.md, it.md …).
 * 2.  First run: embeddings are generated and written to `faq_embeddings.json`.
 * 3.  Subsequent runs: cache is loaded instantly, skipping extra API calls.
 *
 * This keeps the JSON‑tool‑calling flow shown earlier while demonstrating
 * persistent vector storage – a key optimisation for production RAG systems.
 */


using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace dotnetbasicrag.Apps
{
    public class BasicRAGChat : IApp
    {
        public string Name => "Basic RAG Chat";
        string? apiKey = Environment.GetEnvironmentVariable("OpenAIKey");
        string apiBase = Environment.GetEnvironmentVariable("OPENAI_API_BASE")?.TrimEnd('/')
                      ?? "https://api.openai.com/v1";
        const string chatModel = "gpt-4.1";
        const string cacheFile = "faq_embeddings.json";

        public async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("OPENAI_API_KEY not set");
                return;
            }
            HttpClient http = new();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            http.Timeout = TimeSpan.FromMinutes(3);


            // -------------------------- load FAQs & embeddings --------------------------
            var faqDir = Path.Combine(AppContext.BaseDirectory, "faqs");
            if (!Directory.Exists(faqDir))
            {
                Console.WriteLine($"No 'faqs' directory found at {faqDir}");
                return;
            }
            var faqFiles = Directory.EnumerateFiles(faqDir, "*.md")
                                   .ToDictionary(p => Path.GetFileNameWithoutExtension(p));

            // -------------------------- JSON tool schema --------------------------------
            var retrieveFaqFunction = new
            {
                name = "retrieve_faq",
                description = "Return the full FAQ markdown text for a given subject.",
                parameters = new
                {
                    type = "object",
                    properties = new { subject = new { type = "string" } },
                    required = new[] { "subject" }
                }
            };

            // -------------------------- chat loop ---------------------------------------
            Console.WriteLine("Ask a question (type 'exit' to quit)…\n");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("You: ");
                Console.ResetColor();
                string? question = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                var messages = new List<object>
                {
                    new { role = "system", content = "You are a helpful support assistant. Use the retrieve_faq tool as needed." },
                    new { role = "system", content = $"Available subject: {string.Join(", ", faqFiles.Keys)}" },
                    new { role = "system", content = $"If no matching subject can be detected, default to customer_support as the subject" },
                    new { role = "user", content = question }
                };

                // --- 1) detect tool call -------------------------------------------------
                var firstReq = new { model = chatModel, functions = new[] { retrieveFaqFunction }, messages, function_call = "auto" };
                using var firstResp = await PostAsync(http, $"{apiBase}/chat/completions", firstReq);
                var firstChoice = firstResp.RootElement.GetProperty("choices")[0];

                if (firstChoice.GetProperty("message").TryGetProperty("function_call", out var functionCall))
                {
                    // Add the assistant's message with the function call to the messages
                    messages.Add(new
                    {
                        role = "assistant",
                        content = firstChoice.GetProperty("message").GetProperty("content").GetString() ?? "",
                        function_call = new
                        {
                            name = functionCall.GetProperty("name").GetString(),
                            arguments = functionCall.GetProperty("arguments").GetString()
                        }
                    });

                    var functionName = functionCall.GetProperty("name").GetString();
                    var arguments = functionCall.GetProperty("arguments").GetString();
                    var subj = JsonDocument.Parse(arguments).RootElement.GetProperty("subject").GetString()!;

                    if (faqFiles.TryGetValue(subj, out var path))
                    {
                        Console.WriteLine($"Retrieving FAQ for subject '{subj}' from {path}");
                        messages.Add(new { role = "function", name = functionName, content = await File.ReadAllTextAsync(path) });
                    }
                    else
                    {
                        messages.Add(new { role = "function", name = functionName, content = $"Subject '{subj}' not found." });
                    }
                }
                else
                {
                    messages.Add(new
                    {
                        role = "assistant",
                        content = firstChoice.GetProperty("message").GetProperty("content").GetString()
                    });
                }
                // --- 2) final streamed answer -------------------------------------------
                var secondReq = new { model = chatModel, stream = true, messages, temperature = 0.0 };
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Assistant: ");
                Console.ResetColor();
                await foreach (var tok in StreamCompletionAsync(http, secondReq)) Console.Write(tok);
                Console.WriteLine("\n");
            }

        }

        // -------------------------- simple HTTP helpers -----------------------------
        private async Task<JsonDocument> PostAsync(HttpClient http, string url, object body)
        {
            using var resp = await http.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        }

        private async IAsyncEnumerable<string> StreamCompletionAsync(HttpClient http, object payload)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{apiBase}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("data: ")) continue;
                var json = line[6..];
                if (json == "[DONE]") yield break;
                var chunk = JsonDocument.Parse(json);
                var delta = chunk.RootElement.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var c)) yield return c.GetString()!;
            }
        }
    }
}
