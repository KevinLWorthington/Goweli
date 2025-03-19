using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using OpenLibraryNET.Loader;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Goweli.Services
{
    public class BookCoverService : ObservableObject
    {
        private readonly HttpClient _client;
        private int _currentCoverIndex = 0;
        private List<string> _coverEditionKeys = new List<string>();
        private TaskCompletionSource<bool>? _userDecisionTcs;
        private string? _validatedCoverUrl;

        public BookCoverService(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private Bitmap? _previewCoverImage;
        public Bitmap? PreviewCoverImage
        {
            get => _previewCoverImage;
            set => SetProperty(ref _previewCoverImage, value);
        }

        private string? _previewCoverUrl;
        public string? PreviewCoverUrl
        {
            get => _previewCoverUrl;
            set => SetProperty(ref _previewCoverUrl, value);
        }

        private bool _isPreviewVisible;
        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set => SetProperty(ref _isPreviewVisible, value);
        }

        private bool _isProcessingCovers;
        public bool IsProcessingCovers
        {
            get => _isProcessingCovers;
            set => SetProperty(ref _isProcessingCovers, value);
        }

        public async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                _client.Timeout = TimeSpan.FromSeconds(15);

                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
                    title,
                    new KeyValuePair<string, string>("limit", "20")
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    return null;
                }

                _coverEditionKeys.Clear();
                _currentCoverIndex = 0;

                foreach (var result in searchResults)
                {
                    if (result.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                        coverEditionKeyObj != null && !string.IsNullOrEmpty(coverEditionKeyObj.ToString()))
                    {
                        string coverEditionKey = coverEditionKeyObj.ToString();
                        _coverEditionKeys.Add(coverEditionKey);
                    }
                    else if (result.ExtensionData.TryGetValue("cover_i", out var coverId) &&
                            coverId != null && !string.IsNullOrEmpty(coverId.ToString()))
                    {
                        string coverIdKey = coverId.ToString();
                        _coverEditionKeys.Add("ID:" + coverIdKey);
                    }
                }

                if (_coverEditionKeys.Count == 0)
                {
                    return null;
                }

                await DisplayCoverAtCurrentIndexAsync();

                _userDecisionTcs = new TaskCompletionSource<bool>();
                await _userDecisionTcs.Task;

                return _validatedCoverUrl;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task DisplayCoverAtCurrentIndexAsync()
        {
            if (_currentCoverIndex >= _coverEditionKeys.Count)
            {
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
                return;
            }

            try
            {
                IsProcessingCovers = true;
                string coverEditionKey = _coverEditionKeys[_currentCoverIndex];
                string coverUrl;

                if (coverEditionKey.StartsWith("ID:"))
                {
                    string coverId = coverEditionKey.Substring(3);
                    coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
                }
                else
                {
                    coverUrl = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg";
                }

                var imageResponse = await _client.GetByteArrayAsync(coverUrl);

                if (imageResponse.Length < 1000)
                {
                    _currentCoverIndex++;
                    await DisplayCoverAtCurrentIndexAsync();
                    return;
                }

                using var memoryStream = new MemoryStream(imageResponse);
                PreviewCoverImage = new Bitmap(memoryStream);
                PreviewCoverUrl = coverUrl;

                IsPreviewVisible = true;
            }
            catch (Exception)
            {
                await Task.Delay(500);
                _currentCoverIndex++;
                await DisplayCoverAtCurrentIndexAsync();
            }
            finally
            {
                IsProcessingCovers = false;
            }
        }

        public void AcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
            _userDecisionTcs?.TrySetResult(true);
        }

        public async Task RejectCover()
        {
            _currentCoverIndex++;
            if (_currentCoverIndex < _coverEditionKeys.Count)
            {
                await DisplayCoverAtCurrentIndexAsync();
            }
            else
            {
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
            }
        }
    }
}
