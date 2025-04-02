using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Goweli.ViewModels;
using Goweli.Views;
using Goweli.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Goweli
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        public override void Initialize()
        {
                AvaloniaXamlLoader.Load(this);            
        }

        public override void OnFrameworkInitializationCompleted()
        {

                // Register services
                var services = new ServiceCollection();
                RegisterServices(services);
                ServiceProvider = services.BuildServiceProvider();

                // Initialize database
                InitializeDatabaseAsync();

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainViewModel(ServiceProvider)
                    };
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                {
                var mainView = new MainView(ServiceProvider);
                var mainViewModel = new MainViewModel(ServiceProvider);
                mainView.DataContext = mainViewModel;
                singleViewPlatform.MainView = mainView;
            }

                base.OnFrameworkInitializationCompleted();
        }

        private static void RegisterServices(IServiceCollection services)
        {

            services.AddSingleton(sp =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5128/")
                };
                return httpClient;
            });
                // Register database services
                services.AddSingleton<IDatabaseService, DatabaseService>();

        }

        private async void InitializeDatabaseAsync()
        {
            var databaseService = ServiceProvider?.GetRequiredService<IDatabaseService>();
            if (databaseService != null)
            {
                _ = databaseService.InitializeAsync();
            }
        }
    }
}
