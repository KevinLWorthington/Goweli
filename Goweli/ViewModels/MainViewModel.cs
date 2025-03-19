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
        catch (Exception ex)
        {
        }
    }

    // Commands for changing views
    [RelayCommand]
    private void ShowAddBookView()
    {
        try
        {
            CurrentViewModel = new AddBookViewModel(this, _dbContext);
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    private void ShowViewBooks()
    {
        try
        {
            CurrentViewModel = new ViewBooksViewModel(this, _dbContext);
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    private void ShowDefault()
    {
        try
        {
            CurrentViewModel = new HomeViewModel();
            ClearBookCover();
        }
        catch (Exception ex)
        {
        }
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
        catch (Exception ex)
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
}