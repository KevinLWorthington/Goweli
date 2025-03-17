using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Goweli.ViewModels;
using Goweli.Views;
using Goweli.Services;
using Goweli.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;

namespace Goweli
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

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
                        DataContext = new MainViewModel(ServiceProvider.GetRequiredService<GoweliDbContext>(), ServiceProvider)
                    };
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                {
                    var mainView = new MainView(ServiceProvider);
                    var mainViewModel = new MainViewModel(ServiceProvider.GetRequiredService<GoweliDbContext>(), ServiceProvider);
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

        private void RegisterServices(IServiceCollection services)
        {
            try
            {
                Console.WriteLine("Registering services...");
                // Register database services
                services.AddSingleton<IDatabaseService, DatabaseService>();

                // Register GoweliDbContext
                services.AddDbContext<GoweliDbContext>(options =>
                    options.UseSqlite("Data Source=file:goweli.db?mode=memory&cache=shared"));

                // Register other services here as needed
                Console.WriteLine("Services registered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering services: {ex.Message}");
            }
        }

        // In your App.axaml.cs or where you initialize your database
        private async void InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("Initializing database...");

                var dbContext = ServiceProvider?.GetRequiredService<GoweliDbContext>();
                if (dbContext != null)
                {
                    // This will create the database if it doesn't exist
                    bool created = await dbContext.Database.EnsureCreatedAsync();

                    if (created)
                    {
                        Console.WriteLine("Database was created");
                    }
                    else
                    {
                        Console.WriteLine("Database already exists");
                    }

                    // Verify we can access the Books table
                    try
                    {
                        var count = await dbContext.Books.CountAsync();
                        Console.WriteLine($"Database has {count} books");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error accessing Books table: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }
    }
}
