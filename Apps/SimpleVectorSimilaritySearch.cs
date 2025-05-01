using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace dotnetbasicrag.Apps
{
    public class SimpleVectorSimilaritySearch : IApp
    {
        public string Name => "Simple Vector Similarity Search";
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiKey = Environment.GetEnvironmentVariable("OpenAIKey");
        private const string apiUrl = "https://api.openai.com/v1/embeddings";
        private const string embeddingsFilePath = "embeddings.json";

        public async Task ExecuteAsync()
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            // Generate an array of 100 random strings
            string[] randomStrings = AnimalList().ToArray();

            // Dictionary to store embeddings and corresponding strings
            Dictionary<float[], string> embeddingsDictionary = new Dictionary<float[], string>();

            //Load embeddings from file if it exists, otherwise generate embeddings and save to file
            if (File.Exists(embeddingsFilePath))
            {
                embeddingsDictionary = LoadEmbeddingsFromFile(embeddingsFilePath);
            }
            else
            {
                foreach (var str in randomStrings)
                {
                    Console.WriteLine($"Generating embedding for: {str}");
                    var embedding = await GetEmbeddingAsync(str);
                    embeddingsDictionary[embedding] = str;
                }

                SaveEmbeddingsToFile(embeddingsDictionary, embeddingsFilePath);
            }


            while (true)
            {
                Console.WriteLine("Enter a word to find the closest string (or type 'exit' to quit):");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                var inputEmbedding = await GetEmbeddingAsync(input);
                string jsonArray = JsonSerializer.Serialize(inputEmbedding);
                Console.WriteLine($"The embedding for {input} is : {jsonArray}");
                var closestString = FindClosestString(inputEmbedding, embeddingsDictionary);

                Console.WriteLine($"Closest string: {closestString}");
            }
            Console.ReadLine();
        }

        public List<string> AnimalList()
        {
            return new List<string>
        {
            "Aardvark",
            "Albatross",
            "Alligator",
            "Alpaca",
            "Ant",
            "Anteater",
            "Antelope",
            "Ape",
            "Armadillo",
            "Donkey",
            "Baboon",
            "Badger",
            "Barracuda",
            "Bat",
            "Bear",
            "Beaver",
            "Bee",
            "Bison",
            "Boar",
            "Buffalo",
            "Butterfly",
            "Camel",
            "Capybara",
            "Caribou",
            "Cassowary",
            "Cat",
            "Caterpillar",
            "Cattle",
            "Chamois",
            "Cheetah",
            "Chicken",
            "Chimpanzee",
            "Chinchilla",
            "Chough",
            "Clam",
            "Cobra",
            "Cockroach",
            "Cod",
            "Cormorant",
            "Coyote",
            "Crab",
            "Crane",
            "Crocodile",
            "Crow",
            "Curlew",
            "Deer",
            "Dinosaur",
            "Dog",
            "Dogfish",
            "Dolphin",
            "Dotterel",
            "Dove",
            "Dragonfly",
            "Duck",
            "Dugong",
            "Dunlin",
            "Eagle",
            "Echidna",
            "Eel",
            "Eland",
            "Elephant",
            "Elk",
            "Emu",
            "Falcon",
            "Ferret",
            "Finch",
            "Fish",
            "Flamingo",
            "Fly",
            "Fox",
            "Frog",
            "Gaur",
            "Gazelle",
            "Gerbil",
            "Giraffe",
            "Gnat",
            "Gnu",
            "Goat",
            "Goldfinch",
            "Goldfish",
            "Goose",
            "Gorilla",
            "Goshawk",
            "Grasshopper",
            "Grouse",
            "Guanaco",
            "Gull",
            "Hamster",
            "Hare",
            "Hawk",
            "Hedgehog",
            "Heron",
            "Herring",
            "Hippopotamus",
            "Hornet",
            "Horse",
            "Human",
            "Hummingbird",
            "Hyena",
            "Ibex",
            "Ibis",
            "Jackal",
            "Jaguar",
            "Jay",
            "Jellyfish"
        };
        }

        private async Task<float[]> GetEmbeddingAsync(string input)
        {
            var requestBody = new
            {
                model = "text-embedding-3-large",
                input = input
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, jsonOptions);

            return embeddingResponse.data[0].embedding;
        }

        private static string FindClosestString(float[] inputEmbedding, Dictionary<float[], string> embeddingsDictionary)
        {
            float maxSimilarity = float.MinValue;
            string closestString = null;

            foreach (var kvp in embeddingsDictionary)
            {
                float similarity = CosineSimilarity(inputEmbedding, kvp.Key);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    closestString = kvp.Value;
                }
            }

            return closestString;
        }

        private static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = (float)Math.Sqrt(magnitudeA);
            magnitudeB = (float)Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }

        private static void SaveEmbeddingsToFile(Dictionary<float[], string> embeddingsDictionary, string filePath)
        {
            var serializableEmbeddings = embeddingsDictionary.ToDictionary(
                kvp => string.Join(",", kvp.Key),
                kvp => kvp.Value
            );

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(serializableEmbeddings, jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }

        private static Dictionary<float[], string> LoadEmbeddingsFromFile(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            var serializableEmbeddings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            return serializableEmbeddings.ToDictionary(
                kvp => kvp.Key.Split(',').Select(float.Parse).ToArray(),
                kvp => kvp.Value
            );
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
