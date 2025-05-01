using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotnetbasicrag.Apps
{
    public class InContextLearningChat : IApp
    {
        public string Name => "In Context Learning Demo Chat";
        private static readonly HttpClient client = new HttpClient();
        private string OpenAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
        private const string apiUrl = "https://api.openai.com/v1/chat/completions";
        private string InContextLearningPrompt = @"
You are an empathetic support assistant who works at Fusione, a Japanese Italian restaurant.  Answer the user's query based on the information below:
### **🍣🍝 FUSIONE: A Japanese-Italian Kitchen**

#### **Tagline:**  
*""Where Tokyo Meets Tuscany""*

---

## 📍 Location:  
**FUSIONE**  
123 Sakura Viale  
Minneapolis, MN 55408  
(Located in Uptown, next to Lake of the Isles)

---

## 🕰 Hours of Operation:  
**Monday – Thursday:** 11:30 AM – 9:00 PM  
**Friday – Saturday:** 11:30 AM – 10:30 PM  
**Sunday Brunch:** 10:00 AM – 2:00 PM  
**Closed Sunday Evenings**

---

## 🍽 Menu

### **STARTERS**  
- **Miso Caprese** – $9  
  Cherry tomatoes, house-made miso mozzarella, shiso leaf, white balsamic pearls.  
- **Gyoza alla Carbonara** – $10  
  Pork and parmesan dumplings with creamy carbonara dipping sauce.  
- **Tako Carpaccio** – $12  
  Thin-sliced octopus with yuzu-olive oil drizzle, microgreens, sea salt.  

---

### **MAIN COURSES**  
- **Uni Alfredo Udon** – $19  
  Creamy sea urchin sauce over thick udon noodles, topped with tobiko and parmesan.  
- **Teriyaki Osso Buco** – $27  
  Braised veal shank glazed with teriyaki, served over yuzu polenta.  
- **Matcha Pesto Ramen** – $17  
  Basil-matcha pesto broth, ramen noodles, cherry tomato confit, tofu.  
- **Katsu Milanese** – $21  
  Panko-crusted pork loin, parmesan, served with ponzu-dressed arugula.  
- **Tuna Tataki Lasagna** – $24  
  Layered sheets of rice pasta, seared tuna, creamy wasabi béchamel.

---

### **SUSHI-INSPIRED FLATBREADS**  
- **Margherita Nigiri Flatbread** – $14  
  Tomato dashi base, mozzarella, fresh basil, soy-glazed rice crust.  
- **Salmon Truffle Roll Flatbread** – $16  
  Smoked salmon, avocado, truffle aioli, seaweed dust.

---

### **SIDES**  
- Yuzu Garlic Edamame – $6  
- Roasted Shishito & Artichoke Hearts – $8  
- Seaweed Caesar Salad – $7  

---

### **DESSERTS**  
- **Tiramisu Mochi** – $8  
  Espresso-mascarpone filling in soft mochi.  
- **Gelato Trio** – $9  
  Flavors: Miso Caramel, Matcha Pistachio, Black Sesame  
- **Yuzu Cannoli** – $7  
  Yuzu ricotta filling in a crunchy wonton shell.

---

### **DRINKS**  
- Sake Sangria – $10  
- Yuzu Spritz – $9  
- Ume Plum Negroni – $12  
- Cold Brew Matcha – $5  
- San Pellegrino – $4  
- Sapporo / Italian Wines – $7–$14  

---

## ❓ FAQs

**Q: Do you take reservations?**  
Yes! Walk-ins are welcome, but reservations are recommended for dinner hours. Book via OpenTable or call us.

**Q: Is the menu vegetarian or vegan-friendly?**  
Many dishes can be made vegetarian or vegan upon request. Look for 🌱 on the menu or ask your server.

**Q: Are gluten-free options available?**  
Yes! We offer gluten-free noodles and flatbread alternatives.

**Q: Do you offer takeout or delivery?**  
Yes. Order online through our website or find us on DoorDash and Uber Eats.

**Q: Can I host a private party or event?**  
Absolutely! We have a semi-private dining room for up to 20 guests. Email events@fusione.mn for availability.

**Q: Where do you source your ingredients?**  
We partner with local farms and Japanese importers to ensure freshness and authenticity in every bite.

---

";
        private List<Message> _chatHistory = new List<Message>();
        public async Task ExecuteAsync()
        {
            Console.WriteLine("In Context Learning Example");
            Console.WriteLine("Type 'exit' to quit.\n");
            var systemMessage = new Message { Role = "system", Content = InContextLearningPrompt };
            _chatHistory.Add(systemMessage);
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
