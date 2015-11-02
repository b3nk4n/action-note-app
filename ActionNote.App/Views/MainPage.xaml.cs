using System;
using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using ActionNote.Common.Models;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UniversalPage, MainViewModelCallbacks, INoteControlViewModelCallbacks
    {
        public MainPage()
        {
            InitializeComponent();

            DataContext = new MainViewModel(this);
        }

        public void ShowEditView(NoteItem noteItem)
        {
            var noteViewModel = new NoteControlViewModel(this, noteItem);
            EditControl.DataContext = noteViewModel;

            EditControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        public void HideEditView()
        {
            EditControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //EditControl.DataContext = null;
        }

        public void NoteSaved(NoteItem noteItem)
        {
            // refresh data context
            (DataContext as MainViewModel).NoteItems.Add(noteItem); // TODO: fixme! ugly! merge mainviewmodel and notecontrolviewmodel? not when edited !!!

            EditControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //EditControl.DataContext = null; // this call caused issues with colors!
        }

        public void NoteUpdated(NoteItem noteItem)
        {
            EditControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //EditControl.DataContext = null; // this call caused issues with colors!
        }

        public void NoteDiscared()
        {
            EditControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //EditControl.DataContext = null;
        }
    }
}
