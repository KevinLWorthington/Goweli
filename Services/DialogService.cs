using Avalonia.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Goweli.Services
{
    public class DialogService : IDialogService
    {
        public async Task<bool> ShowConfirmationDialog(string message, string title)
        {
            var buttons = new List<DialogButton>
            {
                new("Yes", true),
                new("No", false)
            };

            var result = await ShowDialog(message, title, buttons);
            return result.Result ?? false;
        }

        public async Task<DialogResult> ShowDialog(string message, string title, IEnumerable<DialogButton> buttons)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            foreach (var button in buttons)
            {
                var btn = new Button
                {
                    Content = button.Content,
                    Width = 80,
                    Margin = new Thickness(5)
                };
                btn.Click += (_, __) => window.Close(new DialogResult { Result = button.Result });
                buttonsPanel.Children.Add(btn);
            }

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(buttonsPanel);

            window.Content = stackPanel;

            var resultObj = await window.ShowDialog<DialogResult>(owner: ActiveWindow);

            return resultObj;
        }

        private static Window? ActiveWindow
        {
            get
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    foreach (var window in desktop.Windows)
                    {
                        if (window.IsActive)
                        {
                            return window;
                        }
                    }

                    // Fallback to the main window if none are active
                    return desktop.MainWindow;
                }

                return null;
            }
        }
    }
}

