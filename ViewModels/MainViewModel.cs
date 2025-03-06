using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Services;
using System.Diagnostics;
using System;
using System.IO;
using System.Net.Http;

namespace Goweli.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object _CurrentViewModel;

    [ObservableProperty]
    private Bitmap? _bookCoverImage;

    [ObservableProperty]
    private bool _isBookCoverVisible;

    public RelayCommand AddBookViewCommand { get; }
    public RelayCommand ViewBooksCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowDefaultViewCommand { get; } 

    public async void LoadBookCover(string coverUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync(coverUrl);
            _bookCoverImage = new Bitmap(stream);
            IsBookCoverVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading image: {ex.Message}");
            ClearBookCover();
        }
    }

    public void ClearBookCover()
    {
        BookCoverImage = null;
        IsBookCoverVisible = false;
    }

    private void ShowAddBookView()
    {
        CurrentViewModel = new AddBookViewModel(this);
    }

    private void ShowViewBooksView()
    {
        CurrentViewModel = new ViewBooksViewModel(this, new DialogService());
    }

    private void ShowSearchView()
    {
        CurrentViewModel = new SearchViewModel();
    }
    
    public void ShowDefaultView()
    {
        CurrentViewModel = new HomeViewModel();
    }
}
