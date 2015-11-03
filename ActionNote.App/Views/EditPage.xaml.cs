using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditPage : UniversalPage
    {
        public EditPage()
            : base()
        {
            InitializeComponent();
            DataContext = new EditViewModel();
        }
    }
}
