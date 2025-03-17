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
        Console.WriteLine("Starting Avalonia WASM application");

        try
        {
            // Start the Avalonia application
            await BuildAvaloniaApp()
                .UseReactiveUI()
                .StartBrowserAppAsync("out");

            Console.WriteLine("Avalonia WASM application started successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error starting application: {ex}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .WithInterFont();
    }
}