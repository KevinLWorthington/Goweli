using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Goweli.Views
{
    public partial class ConfirmationDialog : UserControl
    {
        public ConfirmationDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
