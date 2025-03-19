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
        }

        private void RegisterServices(IServiceCollection services)
        {
                // Register database services
                services.AddSingleton<IDatabaseService, DatabaseService>();

                // Register GoweliDbContext
                services.AddDbContext<GoweliDbContext>(options =>
                    options.UseSqlite("Data Source=file:goweli.db?mode=memory&cache=shared"));

        }

        private async void InitializeDatabaseAsync()
        {

                var dbContext = ServiceProvider?.GetRequiredService<GoweliDbContext>();
                if (dbContext != null)
                {
                    // This will create the database if it doesn't exist
                    bool created = await dbContext.Database.EnsureCreatedAsync();

                    // Verify we can access the Books table
                    
                        var count = await dbContext.Books.CountAsync();                    
                }            
        }
    }
}
