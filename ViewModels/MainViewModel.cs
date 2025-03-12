using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

    private readonly HttpClient _httpClient;

    public MainViewModel()
    {
        // Initialize the default view when the application starts
        try
        {
            Console.WriteLine("MainViewModel constructor starting");
            _httpClient = new HttpClient();
            IsBookCoverVisible = false; // Start with cover hidden
            ShowDefault();
            Console.WriteLine("MainViewModel constructor completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MainViewModel constructor: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowAddBookView()
    {
        try
        {
            Console.WriteLine("ShowAddBookView command executed");
            CurrentViewModel = new AddBookViewModel(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowAddBookView: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowViewBooks()
    {
        try
        {
            Console.WriteLine("ShowViewBooks command executed");
            CurrentViewModel = new ViewBooksViewModel(this);
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
            // Clear any displayed book cover when returning to home
            ClearBookCover();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowDefault: {ex.Message}");
        }
    }

    public async Task LoadBookCoverAsync(string coverUrl)
    {
        if (string.IsNullOrEmpty(coverUrl))
        {
            Console.WriteLine("Cover URL is empty, not loading image");
            ClearBookCover();
            return;
        }

        try
        {
            Console.WriteLine($"MainViewModel.LoadBookCoverAsync: Loading from URL: {coverUrl}");
            using var response = await _httpClient.GetAsync(coverUrl);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();

            var bitmap = new Bitmap(stream);

            // Update the properties on the UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                BookCoverImage = bitmap;
                IsBookCoverVisible = true;
                Console.WriteLine("Cover image set and visible");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {ex.Message}");
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ClearBookCover();
            });
        }
    }

    public void ClearBookCover()
    {
        BookCoverImage = null;
        IsBookCoverVisible = false;
        Console.WriteLine("Book cover cleared");
    }
}