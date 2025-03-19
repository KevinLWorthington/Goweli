using Avalonia.Media;
using Avalonia;
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
    // Properties to be called from the view
    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private Bitmap? _bookCoverImage;

    [ObservableProperty]
    private bool _isBookCoverVisible;

    [ObservableProperty]
    private bool _isMenuVisible;
        
    private readonly HttpClient _httpClient;
    private readonly GoweliDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    // Constructor for dependency injection
    public MainViewModel(GoweliDbContext dbContext, IServiceProvider serviceProvider)
    {
        try
        {
            _httpClient = new HttpClient();
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            IsBookCoverVisible = false; // Start with cover hidden
            ShowDefault();
        }
        catch (Exception)
        {
        }
    }

    // Commands for changing views
    [RelayCommand]
    private void ShowAddBookView()
    {
        CurrentViewModel = new AddBookViewModel(this, _dbContext);        
    }

    [RelayCommand]
    private void ShowViewBooks()
    {
            CurrentViewModel = new ViewBooksViewModel(this, _dbContext);
    }

    [RelayCommand]
    private void ShowDefault()
    {
            CurrentViewModel = new HomeViewModel();
            ClearBookCover();
    }

    // Method to load the book cover
    public async Task LoadBookCoverAsync(string coverUrl)
    {
        if (string.IsNullOrEmpty(coverUrl))
        {
            ClearBookCover();
            return;
        }

        try
        {
            using var response = await _httpClient.GetAsync(coverUrl);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();

            var bitmap = new Bitmap(stream);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                BookCoverImage = bitmap;
                IsBookCoverVisible = true;
            });
        }
        catch (Exception)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ClearBookCover();
            });
        }
    }
    
    // Method to clear the book cover
    public void ClearBookCover()
    {
        BookCoverImage = null;
        IsBookCoverVisible = false;
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        {
            IsMenuVisible = !IsMenuVisible;
        }
    }
}