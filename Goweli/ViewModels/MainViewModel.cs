using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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

    // Fields for dependency injection
    private readonly HttpClient _httpClient;
    private readonly IDatabaseService _databaseService;
    private readonly IServiceProvider _serviceProvider;

    // Constructor for dependency injection
    public MainViewModel(IServiceProvider serviceProvider)
    {
        try
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _httpClient = serviceProvider.GetRequiredService<HttpClient>(); // Get from DI
            _databaseService = serviceProvider.GetRequiredService<IDatabaseService>(); // Get from DI
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
        CurrentViewModel = new AddBookViewModel(this, _databaseService, _httpClient);        
    }

    [RelayCommand]
    private void ShowViewBooks()
    {
            CurrentViewModel = new ViewBooksViewModel(this, _databaseService);
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

        // Initiate http connection and load book cover from url stored in database
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

    // Method to toggle the menu
    [RelayCommand]
    private void ToggleMenu()
    {
        {
            IsMenuVisible = !IsMenuVisible;
        }
    }
}