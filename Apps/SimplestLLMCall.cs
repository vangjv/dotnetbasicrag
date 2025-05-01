using System.Text;
using System.Text.Json;

namespace dotnetbasicrag.Apps
{
    public class SimplestLLMCall : IApp
    {
        public string Name => "Simplest LLM Call in C#";
        public async Task ExecuteAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("OpenAIKey")}");
            var payload = JsonSerializer.Serialize(new
            {
                model = "gpt-4.1-mini",
                messages = new[] { 
                    new { role = "system", content = "You are a relatively helpful assistant. Respond like a pirate." },
                    new { role = "user", content = "Hello world!" }
                }
            });
            var res = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );
            Console.WriteLine(await res.Content.ReadAsStringAsync());
            Console.ReadLine();
        }
    }
}
