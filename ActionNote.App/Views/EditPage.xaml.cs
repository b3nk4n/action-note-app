﻿using System;
using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditPage : UniversalPage, EditViewModelCallbacks
    {
        public EditPage()
            : base(typeof(MainPage))
        {
            InitializeComponent();
            DataContext = new EditViewModel(this);
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
            if (e.Key == VirtualKey.Enter)
            {
                ContentTextBox.Focus(FocusState.Programmatic);
                ContentTextBox.Select(ContentTextBox.Text.Length, 0);
            }
        }
    }
}
