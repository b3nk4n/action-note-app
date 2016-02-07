using ActionNote.App.ViewModels;
using System;
using UWPCore.Framework.Controls;
using UWPCore.Framework.Mvvm;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SharePage : Page
    {
        public SharePage()
        {
            InitializeComponent();
            DataContext = new ShareViewModel();
        }

        

        //protected async override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);

        //    var shareOperation = e.Parameter as ShareOperation;
        //    if (shareOperation != null)
        //    {
        //        if (shareOperation.Data.Contains(StandardDataFormats.Text))
        //        {
        //            TitleTextBox.Text += StandardDataFormats.Text;
        //            ContentTextBox.Text += await shareOperation.Data.GetTextAsync();
        //        }
        //        else if (shareOperation.Data.Contains(StandardDataFormats.WebLink))
        //        {
        //            TitleTextBox.Text += StandardDataFormats.WebLink;
        //            var uri = await shareOperation.Data.GetWebLinkAsync();
        //            ContentTextBox.Text += uri.AbsoluteUri;
        //        }
        //    }
        //}

        // TODO: ...
        // save note
        // dismiss
        // flag as important
        // color category
        // attachement image?

        // UniversalSharePage?
        // INavigationService => Basic/NullNavigationService which simply uses the Frame to navigate?
        
    }
}
