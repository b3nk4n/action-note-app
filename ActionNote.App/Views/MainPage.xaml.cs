using System;
using ActionNote.App.ViewModels;
using ActionNote.Common.Models;
using Universal.UI.Xaml.Controls;
using UWPCore.Framework.Controls;
using UWPCore.Framework.Common;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using ActionNote.Common.Services;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Documents;
using UWPCore.Framework.Launcher;
using Windows.UI.Xaml;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UniversalPage, IMainViewModelCallbacks
    {
        private ITilePinService _tilePinService;

        private MainViewModel ViewModel { get; set; }

        private Localizer _localizer = new Localizer();

        public MainPage()
        {
            InitializeComponent();

            _tilePinService = Injector.Get<ITilePinService>();

            ViewModel = new MainViewModel(this);
            DataContext = ViewModel;
        }

        private void SwipeListView_ItemSwipe(object sender, ItemSwipeEventArgs e)
        {
            var item = e.SwipedItem as NoteItem;
            if (item != null)
            {
                if (e.Direction == SwipeListDirection.Left)
                {
                    item.IsImportant = !item.IsImportant;
                }
                else
                {
                    ViewModel.RemoveCommand.Execute(item);
                }
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

        private void NoteListItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).Tag as NoteItem;
            if (item != null)
            {
                ViewModel.EditCommand.Execute(item);
            }
        }

        private async void NoteListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
                return;

            var item = (sender as FrameworkElement).Tag as NoteItem;

            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(_localizer.Get("Delete.Label"), (command) =>
            {
                ViewModel.RemoveCommand.Execute(item);
            }));
            menu.Commands.Add(new UICommand(_localizer.Get("Share.Label"), (command) =>
            {
                ViewModel.ShareCommand.Execute(item);
            }));

            if (_tilePinService.Contains(item.Id))
            {
                menu.Commands.Add(new UICommand(_localizer.Get("Unpin.Label"), (command) =>
                {
                    ViewModel.UnpinCommand.Execute(item);
                }));
            }
            else
            {
                menu.Commands.Add(new UICommand(_localizer.Get("Pin.Label"), (command) =>
                {
                    ViewModel.PinCommand.Execute(item);
                }));
            }

            var point = e.GetPosition(null);
            point.X += 66;
            await menu.ShowAsync(point);
        }

        public void SyncStarted()
        {
            SyncAnimation.Begin();
        }

        private async void RichTextTapped(object sender, TappedRoutedEventArgs e)
        {
            var richTextBox = sender as RichTextBlock;

            if (richTextBox != null)
            {
                var textPointer = richTextBox.GetPositionFromPoint(e.GetPosition(richTextBox));
                var element = textPointer.Parent as TextElement;

                if (element is Run)
                {
                    var run = (Run)element;
                    if (run.Text.StartsWith("#"))
                    {
                        e.Handled = true;
                    }
                    else if (run.Text.StartsWith("http://") || run.Text.StartsWith("https://") || run.Text.StartsWith("www."))
                    {
                        var link = run.Text;
                        if (link.StartsWith("www."))
                            link = "http://" + link;

                        Uri uriResult;
                        bool isValidUri = Uri.TryCreate(link, UriKind.Absolute, out uriResult);

                        if (isValidUri)
                        {
                            e.Handled = true;
                            await SystemLauncher.LaunchUriAsync(new Uri(link, UriKind.Absolute));
                        }
                    }
                }
            }
        }
    }
}
