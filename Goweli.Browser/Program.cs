using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using Goweli;

internal class Program
{
    public static async Task Main(string[] args)
    {

        try
        {
            Console.WriteLine("Starting Goweli Browser application...");

            // Start the Avalonia application
            await BuildAvaloniaApp()
                .UseReactiveUI()
                .StartBrowserAppAsync("out");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .WithInterFont();
    }
}