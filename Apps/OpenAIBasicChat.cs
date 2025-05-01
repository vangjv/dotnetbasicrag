using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotnetbasicrag.Apps
{
    public class OpenAIBasicChat : IApp
    {
        public string Name => "OpenAI Basic Chat";
        private static readonly HttpClient client = new HttpClient();
        private string OpenAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
        private const string apiUrl = "https://api.openai.com/v1/chat/completions";
        private readonly List<Message> _chatHistory = new List<Message>
        {
            new Message { Role = "system", Content = "You are a helpful assistant. Whenever the user mentions the secret word of the day 'bumblebee'.  Celebrate like there is no tomorrow!" }
        };

        public async Task ExecuteAsync()
        {
            Console.WriteLine("OpenAI Chat Assistant with History and Streaming");
            Console.WriteLine("Type 'exit' to quit.\n");

            while (true)
            {
                Console.WriteLine("You: ");
                string userInput = Console.ReadLine();
                if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                _chatHistory.Add(new Message { Role = "user", Content = userInput });
                var response = await GetOpenAIResponse();
                Console.WriteLine($"\n");
                _chatHistory.Add(new Message { Role = "assistant", Content = response });
            }
        }

        private async Task<string> GetOpenAIResponse()
        {
            var requestBody = new
            {
                model = "gpt-4.1",
                messages = _chatHistory.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAIKey}");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = content
                };

                using var httpResponse = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                httpResponse.EnsureSuccessStatusCode();

                using var stream = await httpResponse.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                var assistantMessage = new StringBuilder();
                Console.WriteLine($"\nAssistant:");
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("data: "))
                    {
                        var jsonData = line.Substring(6);
                        if (jsonData == "[DONE]")
                            break;

                        var streamedResponse = JsonSerializer.Deserialize<StreamedResponse>(jsonData);
                        var contentChunk = streamedResponse?.Choices.FirstOrDefault()?.Delta.Content;
                        if (!string.IsNullOrEmpty(contentChunk))
                        {
                            assistantMessage.Append(contentChunk);
                            Console.Write(contentChunk);
                        }
                    }
                }
                return assistantMessage.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private class Message
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        private class StreamedResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("delta")]
            public Delta Delta { get; set; }
        }

        private class Delta
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}
