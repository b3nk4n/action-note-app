using System;
using System.Linq;
using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using ActionNote.Common.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditPage : UniversalPage, EditViewModelCallbacks
    {
        private EditViewModel ViewModel { get; set; }

        public EditPage()
            : base(typeof(MainPage))
        {
            InitializeComponent();
            ViewModel = new EditViewModel(this);
            DataContext = ViewModel;
        }

        public async void SelectTitle()
        {
            if (!string.IsNullOrWhiteSpace(TitleTextBox.Text))
                return;

            // wait required or the selection would be changed afterwards
            await Task.Delay(25);
            TitleTextBox.Focus(FocusState.Programmatic);
        }

        public void UnfocusTextBoxes()
        {
            // we have to ensure that all text boxes are unfocused, because the bindings are trigger on unfocus!
            FocusElement.Focus(FocusState.Programmatic);
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

        private async void FileDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral();
                var files = await e.DataView.GetStorageItemsAsync();
                if (files.Count > 0)
                {
                    var file = files.First() as IStorageFile;
                    var fileTypeLowerCase = file.FileType.ToLower();
                    var isKnownFileType = fileTypeLowerCase == ".png" || fileTypeLowerCase == ".jpg" || fileTypeLowerCase == ".jpeg";
                    e.AcceptedOperation = isKnownFileType ? DataPackageOperation.Copy : DataPackageOperation.None;
                }
                deferral.Complete();
                return;
            }
        }

        private async void FileDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    ViewModel.SaveAttachementCommand.Execute(storageFile);
                }
            }
        }
    }
}
