using System;
using ActionNote.App.ViewModels;
using ActionNote.Common.Models;
using Universal.UI.Xaml.Controls;
using UWPCore.Framework.Common;
using UWPCore.Framework.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArchivPage : UniversalPage
    {
        private ArchivViewModel ViewModel { get; set; }

        private Localizer _localizer = new Localizer();

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

        private async void NoteListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
                return;

            var item = (sender as Rectangle).Tag as NoteItem;

            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(_localizer.Get("Delete.Label"), (command) =>
            {
                ViewModel.RemoveCommand.Execute(item);
            }));

            var point = e.GetPosition(null);
            point.X += 40;
            await menu.ShowAsync(point);
        }
    }
}
