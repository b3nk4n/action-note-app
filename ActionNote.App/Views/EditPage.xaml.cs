using System;
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

        /// <summary>
        /// Perform INTELLIGENT KEYBOARD.
        /// </summary>
        private void ContentTextBoxKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (ContentTextBox.SelectionLength > 1) // TODO do nothing if text is selected?
                    return;

                var selectedIndex = ContentTextBox.SelectionStart;
                var text = ContentTextBox.Text.Replace("\r\n", "\r"); // work on a copy, because CB.electionStart does count '\r\n' only as 1, note to chars!

                var textBeforeEnter = text.Substring(0, selectedIndex);
                var textAfterEnter = text.Substring(selectedIndex);
                var lineBeforeEnter = textBeforeEnter;
                if (textBeforeEnter.EndsWith("\r"))
                    lineBeforeEnter = lineBeforeEnter.Remove(lineBeforeEnter.Length - 1);
                var lastIndex = lineBeforeEnter.LastIndexOfAny(new[] { '\r', '\n' });
                if (lastIndex != -1)
                    lineBeforeEnter = lineBeforeEnter.Substring(lastIndex).TrimStart('\r');

                if (lineBeforeEnter.Length > 0)
                {
                    string tagToInsert = null;
                    if (lineBeforeEnter.StartsWith("- "))
                        tagToInsert = "- ";
                    if (lineBeforeEnter.StartsWith("+ "))
                        tagToInsert = "+ ";
                    if (lineBeforeEnter.StartsWith("* "))
                        tagToInsert = "* ";
                    if (lineBeforeEnter.StartsWith("> "))
                        tagToInsert = "> ";
                    if (lineBeforeEnter.StartsWith("-> "))
                        tagToInsert = "-> ";

                    if (tagToInsert != null &&
                        lineBeforeEnter.Length > tagToInsert.Length)
                    {
                        ContentTextBox.Text = textBeforeEnter + tagToInsert + textAfterEnter;

                        ContentTextBox.Focus(FocusState.Programmatic);
                        ContentTextBox.Select(selectedIndex + tagToInsert.Length, 0);
                    }
                    else // remove start tag
                    {
                        string tagToRemove = null;
                        if (lineBeforeEnter == "- ")
                            tagToRemove = "- ";
                        if (lineBeforeEnter == "+ ")
                            tagToRemove = "+ ";
                        if (lineBeforeEnter == "* ")
                            tagToRemove = "* ";
                        if (lineBeforeEnter == "> ")
                            tagToRemove = "> ";
                        if (lineBeforeEnter == "-> ")
                            tagToRemove = "-> ";

                        if (tagToRemove != null)
                        {
                            ContentTextBox.Text = textBeforeEnter.Remove(textBeforeEnter.Length - (tagToRemove.Length + 1)) + textAfterEnter;

                            ContentTextBox.Focus(FocusState.Programmatic);
                            ContentTextBox.Select(selectedIndex - (tagToInsert.Length + 1), 0);
                        }
                    }
                }
            }
        }
    }
}
