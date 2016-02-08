using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ActionNote.Common.Helpers
{
    public static class TextBoxUtils
    {
        public static void JumpFucusOnEnterTo(TextBox textBox, VirtualKey key)
        {
            if (key == VirtualKey.Enter)
            {
                textBox.Focus(FocusState.Programmatic);
                textBox.Select(textBox.Text.Length, 0);
            }
        }

        public static void IntelligentOnEnter(TextBox textBox, VirtualKey key)
        {
            if (key == VirtualKey.Enter)
            {
                var selectedIndex = textBox.SelectionStart;

                // work on a copy, because CB.electionStart does count '\r\n' only as 1, note to chars!
                var text = textBox.Text.Replace("\r\n", "\r");

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
                        textBox.Text = textBeforeEnter + tagToInsert + textAfterEnter;

                        textBox.Focus(FocusState.Programmatic);
                        textBox.Select(selectedIndex + tagToInsert.Length, 0);
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
                            textBox.Text = textBeforeEnter.Remove(textBeforeEnter.Length - (tagToRemove.Length + 1)) + textAfterEnter;

                            textBox.Focus(FocusState.Programmatic);
                            textBox.Select(selectedIndex - (tagToInsert.Length + 1), 0);
                        }
                    }
                }
            }
        }
    }
}
