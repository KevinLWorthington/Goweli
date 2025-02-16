using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goweli.Services
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationDialog(string message, string title);
        Task<DialogResult> ShowDialog(string message, string title, IEnumerable<DialogButton> buttons);
    }

    public class DialogButton(string content, bool result)
    {
        public string Content { get; set; } = content;
        public bool Result { get; set; } = result;
    }

    public class DialogResult
    {
        public bool? Result { get; set; }
    }
}
