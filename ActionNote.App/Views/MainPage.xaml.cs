using System;
using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using ActionNote.Common.Models;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UniversalPage, MainViewModelCallbacks
    {
        public MainPage()
        {
            InitializeComponent();

            DataContext = new MainViewModel(this);
        }

        public void ShowEditView(NoteItem noteItem)
        {
            EditControl.DataContext = new NoteControlViewModel(noteItem);
            EditControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        public void HideEditView()
        {
            EditControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            EditControl.DataContext = null;
        }
    }
}
