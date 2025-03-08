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
            CurrentViewModel = new AddBookViewModel(this);
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