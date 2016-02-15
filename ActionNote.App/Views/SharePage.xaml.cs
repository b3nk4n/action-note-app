using ActionNote.App.ViewModels;
using ActionNote.Common.Helpers;
using UWPCore.Framework.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SharePage : UniversalPage
    {
        public SharePage()
        {
            InitializeComponent();
            DataContext = new ShareViewModel();
        }

        private void ColorFlyoutClicked(object sender, RoutedEventArgs e)
        {
            ColorFlyout.Hide();
        }

        private void TitleTextBoxKeyUp(object sender, KeyRoutedEventArgs e)
        {
            TextBoxUtils.JumpFucusOnEnterTo(ContentTextBox, e.Key);
        }

        /// <summary>
        /// Perform INTELLIGENT KEYBOARD.
        /// </summary>
        private void ContentTextBoxKeyUp(object sender, KeyRoutedEventArgs e)
        {
            TextBoxUtils.IntelligentOnEnter(ContentTextBox, e.Key);
        }
    }
}
