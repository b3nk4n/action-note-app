using System;
using ActionNote.App.ViewModels;
using ActionNote.Common.Models;
using Universal.UI.Xaml.Controls;
using UWPCore.Framework.Common;
using UWPCore.Framework.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using ActionNote.Common.Helpers;
using UWPCore.Framework.Launcher;
using Windows.Foundation;

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
            : base(typeof(ArchivPage))
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
                else if (e.Direction == SwipeListDirection.Left)
                {
                    ViewModel.RestoreCommand.Execute(item);
                }
            }
        }

        private void NoteListItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).Tag as NoteItem;
            if (item != null)
            {
                ViewModel.ReadOnlyCommand.Execute(item);
            }
        }

        private void RichTextTapped(object sender, TappedRoutedEventArgs e)
        {
            var richTextBox = sender as RichTextBlock;

            if (richTextBox != null)
            {
                var textPointer = richTextBox.GetPositionFromPoint(e.GetPosition(richTextBox));
                var element = textPointer.Parent as TextElement;

                if (element is Run)
                {
                    var text = ((Run)element).Text;
                    RichTextBindingHelper.PerformRichTextAction(text,
                    async (uri) =>
                    {
                        e.Handled = true;
                        await SystemLauncher.LaunchUriAsync(uri);
                    }, (tag) =>
                    {
                        e.Handled = true;
                    });
                }
            }
        }

        #region Context menu

        private void NoteListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
                return;

            var item = (sender as FrameworkElement).Tag as NoteItem;
            var position = e.GetPosition(null);
            ShowContextMenu(item, null, position);
            e.Handled = true;
        }

        private void NoteListItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch ||
                e.HoldingState != Windows.UI.Input.HoldingState.Started)
                return;

            var item = (sender as FrameworkElement).Tag as NoteItem;
            var position = e.GetPosition(null);
            ShowContextMenu(item, null, position);
            e.Handled = true;
        }

        private void ShowContextMenu(NoteItem item, UIElement target, Point offset)
        {
            var contextMenu = CreateContextMenu(item);
            contextMenu.ShowAt(target, offset);
        }

        /// <summary>
        /// Creates the context menu.
        /// </summary>
        /// <param name="item">The associated note item.</param>
        /// <returns>The context menu popup.</returns>
        private MenuFlyout CreateContextMenu(NoteItem item)
        {
            var style = (Style)Application.Current.Resources["MenuFlyoutItemIconTemplate"];
            var menu = new MenuFlyout();
            menu.Items.Add(new MenuFlyoutItem()
            {
                Text = _localizer.Get("Delete.Label"),
                Command = ViewModel.RemoveCommand,
                CommandParameter = item,
                Tag = Application.Current.Resources["Delete"],
                Style = style
            });
            menu.Items.Add(new MenuFlyoutItem()
            {
                Text = _localizer.Get("Restore.Label"),
                Command = ViewModel.RestoreCommand,
                CommandParameter = item,
                Tag = Application.Current.Resources["Undo"],
                Style = style
            });
            return menu;
        }

        #endregion
    }
}
