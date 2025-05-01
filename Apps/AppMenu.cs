namespace dotnetbasicrag.Apps
{
    public class AppMenu
    {
        private readonly List<IApp> _apps;

        public AppMenu()
        {
            _apps = new List<IApp>
            {
                new OpenAIEmbedding(),
                new SimpleVectorSimilaritySearch(),
                new OpenAIBasicChat(),
            };
        }

        public async Task ShowAsync()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Project Apps Menu ===");

                for (int i = 0; i < _apps.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {_apps[i].Name}");
                }
                Console.WriteLine($"{_apps.Count + 1}. Exit");
                Console.Write("Select an option: ");

                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= _apps.Count + 1)
                {
                    if (choice == _apps.Count + 1)
                    {
                        Console.WriteLine("Exiting...");
                        return;
                    }

                    await _apps[choice - 1].ExecuteAsync();
                }
                else
                {
                    Console.WriteLine("Invalid option. Press Enter to try again.");
                    Console.ReadLine();
                }
            }
        }
    }
}