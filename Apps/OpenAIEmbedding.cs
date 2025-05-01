using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace dotnetbasicrag.Apps
{
    public class OpenAIEmbedding : IApp
    {
        public string Name => "OpenAI Embedding Example";
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiKey = Environment.GetEnvironmentVariable("OpenAIKey");
        private const string apiUrl = "https://api.openai.com/v1/embeddings";

        public async Task ExecuteAsync()
        {
            Console.WriteLine("Enter a string to get its embedding:");
            string input = Console.ReadLine();

            var embedding = await GetEmbeddingAsync(input);
            Console.WriteLine("Embedding:");
            Console.WriteLine("[" + string.Join(", ", embedding) + "]");
            Console.ReadLine();
        }

        private async Task<float[]> GetEmbeddingAsync(string input)
        {
            var requestBody = new
            {
                //The text-embedding-3-large model is a larger text-embedding model designed to represent concepts within content such as
                //natural language or code. It generates embeddings with up to 3072 dimensions,
                model = "text-embedding-3-large",
                input = input
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, jsonOptions);

            return embeddingResponse.data[0].embedding;
        }

        public class EmbeddingResponse
        {
            public Datum[] data { get; set; }
        }

        public class Datum
        {
            public float[] embedding { get; set; }
        }
    }
}
