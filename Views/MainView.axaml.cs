using Avalonia;
using Avalonia.Controls;
using Goweli.ViewModels;

namespace Goweli.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = new MainViewModel();
        }
    }
}