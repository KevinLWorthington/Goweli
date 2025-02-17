using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ConfirmationDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private string _title;

        public IRelayCommand YesCommand { get; }
        public IRelayCommand NoCommand { get; }

        public ConfirmationDialogViewModel()
        {
            _message = string.Empty;
            _title = string.Empty;
            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);
        }

        private void OnYes()
        {
            // Handle Yes action
            // Example: Close dialog with a positive result
        }

        private void OnNo()
        {
            // Handle No action
            // Example: Close dialog with a negative result
        }
    }
}