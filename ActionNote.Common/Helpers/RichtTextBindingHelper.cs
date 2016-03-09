using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace ActionNote.Common.Helpers
{
    public class RichTextBindingHelper : DependencyObject // TODO: move to framework?
    {
        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(RichTextBindingHelper),
                new PropertyMetadata(String.Empty, OnTextChanged));

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as RichTextBlock;
            if (control != null)
            {
                control.Blocks.Clear();
                string value = e.NewValue.ToString();


                var paragraph = ParseForRichTextParagrah(value);
                control.Blocks.Add(paragraph);
            }
        }

        static SolidColorBrush accentBrush = new SolidColorBrush(AppConstants.COLOR_ACCENT);

        private static Paragraph ParseForRichTextParagrah(string message)
        {
            var ret = new Paragraph();
            var lines = message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                if (i != 0)
                    ret.Inlines.Add(new LineBreak());

                FormatLine(lines[i], ret);
            }
            return ret;
        }

        private static void FormatLine(string message, Paragraph ret)
        {
            var words = message.Split(new string[] {" ", "\t"}, StringSplitOptions.None);
            foreach (var word in words)
            {
                //if (word.StartsWith("#"))
                //{
                //    ret.Inlines.Add(new Run { Text = word + " ", Foreground = accentBrush });
                //    continue;
                //}
                if (word.StartsWith("http://") || word.StartsWith("https://") || word.StartsWith("www."))
                {
                    var link = word;
                    if (link.StartsWith("www."))
                        link = "http://" + link;

                    Uri uriResult;
                    bool isValidUri = Uri.TryCreate(link, UriKind.Absolute, out uriResult);

                    if (isValidUri)
                    {
                        var ul = new Underline();
                        ul.Inlines.Add(new Run { Text = word + " ", FontWeight = FontWeights.SemiBold });
                        ret.Inlines.Add(ul);
                        continue;
                    }
                    else
                    {
                        for(int len = link.Length - 1; len > 4; len--)
                        {
                            var subLink = link.Substring(0, len);
                            isValidUri = Uri.TryCreate(subLink, UriKind.Absolute, out uriResult);

                            if (isValidUri)
                            {
                                var ul = new Underline();
                                ul.Inlines.Add(new Run { Text = subLink, FontWeight = FontWeights.SemiBold });
                                ret.Inlines.Add(ul);
                                var wordRest = link.Substring(len, link.Length - len);
                                ret.Inlines.Add(new Run { Text = wordRest + " " });
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ret.Inlines.Add(new Run { Text = word + " " });
                }
            }
        }

        public static void PerformRichTextAction(string text, Action<Uri> linkExecuted, Action<string> tagExecuted)
        {
            //if (text.StartsWith("#"))
            //{
            //    tagExecuted?.Invoke(text);
            //}
            //else 
            if (text.StartsWith("http://") || text.StartsWith("https://") || text.StartsWith("www."))
            {
                var link = text;
                if (link.StartsWith("www."))
                    link = "http://" + link;

                Uri uriResult;
                bool isValidUri = Uri.TryCreate(link, UriKind.Absolute, out uriResult);

                if (isValidUri)
                {
                    linkExecuted?.Invoke(uriResult);
                }
            }
        }
    }
}
