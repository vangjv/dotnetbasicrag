using dotnetbasicrag.Apps;

namespace dotnetbasicrage
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var menu = new AppMenu();
            await menu.ShowAsync();
        }
    }
}