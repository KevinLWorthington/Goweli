using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Goweli.Data;
using Goweli.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Goweli.Views
{
    public partial class MainView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;

        public MainView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();

            // Ensure DataContext is set
            if (Design.IsDesignMode == false && DataContext == null)
            {
                DataContext = new MainViewModel(_serviceProvider.GetRequiredService<GoweliDbContext>(), _serviceProvider);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}