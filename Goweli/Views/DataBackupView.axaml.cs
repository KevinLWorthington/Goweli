using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Goweli.Views
{
    public partial class DataBackupView : UserControl
    {
        private Button? _selectAllButton;

        public DataBackupView()
        {
            InitializeComponent();

            // Get references to UI elements
            _selectAllButton = this.FindControl<Button>("SelectAllButton");

            // Add event handler for the Select All button
            if (_selectAllButton != null)
            {
                _selectAllButton.Click += OnSelectAllClick;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            // Find the export JSON TextBox
            var textBox = this.FindControl<TextBox>("ExportJsonTextBox");
            if (textBox != null)
            {
                textBox.SelectAll();
                textBox.Focus();
            }
        }
    }
}