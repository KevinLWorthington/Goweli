using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Goweli.Data;
using Goweli.ViewModels;
using Goweli.Views;
using Goweli.Services;

namespace Goweli
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            using var db = new AppDbContext();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IDialogService dialogService = new DialogService();

                desktop.MainWindow = new MainView
                {
                    DataContext = new MainViewModel(dialogService)
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}