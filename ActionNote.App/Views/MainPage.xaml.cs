using ActionNote.App.ViewModels;
using ActionNote.Common.Models;
using UWPCore.Framework.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UniversalPage
    {
        private MainViewModel ViewModel { get; set; }

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        private void NoteItemClicked(object sender, ItemClickEventArgs e)
        {
            var clickedNoteItem = e.ClickedItem as NoteItem;

            // go to edit page when selected item is clicked again
            if (clickedNoteItem != null &&
                clickedNoteItem == ViewModel.SelectedNote)
            {
                ViewModel.EditCommand.Execute(clickedNoteItem);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            IntroArrowBlick.Begin();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            IntroArrowBlick.Stop();
        }
    }
}
