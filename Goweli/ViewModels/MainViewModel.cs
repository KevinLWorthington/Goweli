using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
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
    private readonly GoweliDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public MainViewModel(GoweliDbContext dbContext, IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("MainViewModel constructor starting");
            _httpClient = new HttpClient();
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
            CurrentViewModel = new AddBookViewModel(this, _dbContext);
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
            CurrentViewModel = new ViewBooksViewModel(this, _dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowViewBooks: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDefault()
    {
        try
        {
            Console.WriteLine("ShowDefault command executed");
            CurrentViewModel = new HomeViewModel();
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