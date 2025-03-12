using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Goweli.ViewModels;
using Goweli.Views;
using System;

namespace Goweli
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            try
            {
                Console.WriteLine("App.Initialize started");
                AvaloniaXamlLoader.Load(this);
                Console.WriteLine("App.Initialize completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in App.Initialize: {ex.Message}");
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                Console.WriteLine("OnFrameworkInitializationCompleted started");

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainViewModel()
                    };
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                {
                    var mainView = new MainView();
                    var mainViewModel = new MainViewModel();
                    mainView.DataContext = mainViewModel;
                    singleViewPlatform.MainView = mainView;
                }

                base.OnFrameworkInitializationCompleted();
                Console.WriteLine("OnFrameworkInitializationCompleted finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnFrameworkInitializationCompleted: {ex.Message}");
                base.OnFrameworkInitializationCompleted();
            }
        }
    }
}