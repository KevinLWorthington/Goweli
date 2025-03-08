using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Goweli.ViewModels;

namespace Goweli.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();

            // Ensure DataContext is set
            if (Design.IsDesignMode == false && DataContext == null)
            {
                DataContext = new MainViewModel();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}