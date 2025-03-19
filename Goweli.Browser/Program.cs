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
            // Start the Avalonia application
            await BuildAvaloniaApp()
                .UseReactiveUI()
                .StartBrowserAppAsync("out");
        }
        catch (Exception)
        {
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .WithInterFont();
    }
}