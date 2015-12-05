using ActionNote.App.ViewModels;
using ActionNote.Common.Models;
using Universal.UI.Xaml.Controls;
using UWPCore.Framework.Controls;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArchivPage : UniversalPage
    {
        private ArchivViewModel ViewModel { get; set; }

        public ArchivPage()
        {
            InitializeComponent();
            ViewModel = new ArchivViewModel();
            DataContext = ViewModel;
        }

        private void SwipeListView_ItemSwipe(object sender, ItemSwipeEventArgs e)
        {
            var item = e.SwipedItem as NoteItem;
            if (item != null)
            {
                if (e.Direction == SwipeListDirection.Right)
                {
                    ViewModel.RemoveCommand.Execute(item);
                }
            }
        }
    }
}
