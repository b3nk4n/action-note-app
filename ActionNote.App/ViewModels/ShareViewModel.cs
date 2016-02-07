using System;
using System.Collections.Generic;
using UWPCore.Framework.Mvvm;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class ShareViewModel : ViewModelBase
    {
        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            var shareOperation = parameter as ShareOperation;
            if (shareOperation != null)
            {
                if (shareOperation.Data.Contains(StandardDataFormats.Text))
                {
                    string title = shareOperation.Data.Properties.Title;
                    string desc = shareOperation.Data.Properties.Description;
                    //TitleTextBox.Text += StandardDataFormats.Text;
                    //ContentTextBox.Text += await shareOperation.Data.GetTextAsync();
                }
                else if (shareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    //TitleTextBox.Text += StandardDataFormats.WebLink;
                    //var uri = await shareOperation.Data.GetWebLinkAsync();
                    //ContentTextBox.Text += uri.AbsoluteUri;
                }
            }
        }

        // TODO: reportCompleted();
    }
}
