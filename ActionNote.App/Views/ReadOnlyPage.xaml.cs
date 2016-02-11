using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using Windows.UI.Xaml.Input;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadOnlyPage : UniversalPage
    {
        private ReadOnlyViewModel ViewModel { get; set; }

        public ReadOnlyPage()
        {
            InitializeComponent();
            ViewModel = new ReadOnlyViewModel();
            DataContext = ViewModel;
        }

        private void AttachementImageTapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.OpenPicture.Execute(null);
        }
    }
}
