using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Services;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Goweli.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private Bitmap? _bookCoverImage;

    [ObservableProperty]
    private bool _isBookCoverVisible;

    // Check if running in browser/WebAssembly environment
    private static readonly bool IsRunningInBrowser =
        OperatingSystem.IsBrowser() ||
        AppContext.TargetFrameworkName?.Contains("Browser") == true;

    public MainViewModel()
    {
        // Initialize the default view when the application starts
        ShowDefault();
    }

    [RelayCommand]
    private void ShowAddBookView()
    {
        try
        {
            Console.WriteLine("ShowAddBookView command executed");

            // Check if we're in WebAssembly and use a simplified version if needed
            if (IsRunningInBrowser)
            {
                // For WebAssembly, use a simplified or mock AddBookViewModel 
                // that doesn't depend on unsupported features
                CurrentViewModel = new AddBookViewModel(this, isWebAssembly: true);
                Console.WriteLine("Created WebAssembly-specific AddBookViewModel");
            }
            else
            {
                // For desktop, use the full implementation
                CurrentViewModel = new AddBookViewModel(this, isWebAssembly: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowAddBookView: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private void ShowViewBooks()
    {
        try
        {
            Console.WriteLine("ShowViewBooks command executed");

            if (IsRunningInBrowser)
            {
                // WebAssembly-specific implementation or mock
                CurrentViewModel = new ViewBooksViewModel(this, new DialogService(), isWebAssembly: true);
            }
            else
            {
                CurrentViewModel = new ViewBooksViewModel(this, new DialogService(), isWebAssembly: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowViewBooks: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Search()
    {
        try
        {
            Console.WriteLine("Search command executed");
            CurrentViewModel = new SearchViewModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Search: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDefault()
    {
        try
        {
            Console.WriteLine("ShowDefault command executed");
            CurrentViewModel = new HomeViewModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowDefault: {ex.Message}");
        }
    }

    public async Task LoadBookCoverAsync(string coverUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync(coverUrl);
            BookCoverImage = new Bitmap(stream);
            IsBookCoverVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {ex.Message}");
            ClearBookCover();
        }
    }

    public void ClearBookCover()
    {
        BookCoverImage = null;
        IsBookCoverVisible = false;
    }
}
