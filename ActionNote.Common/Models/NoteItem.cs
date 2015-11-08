using System;
using System.ComponentModel;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ActionNote.Common.Models
{
    public enum ColorCategory
    {
        [DefaultValue(true)]
        Neutral, // = Transparent, Accent?
        Red,
        Blue,
        Green,
        Yellow,
        Violett,
        Orange
    }

    public class NoteItem : BindableBase, IRepositoryItem<string>
    {
        public string Id {
            get { return _id; }
            set { Set(ref _id, value); }
        }
        private string _id;

        public string Title {
            get { return _title; }
            set { Set(ref _title, value); }
        }
        private string _title;

        public string Content {
            get { return _content; }
            set { Set(ref _content, value); }
        }
        private string _content;

        public ColorCategory Color {
            get { return _color; }
            set
            {
                Set(ref _color, value);
            }
        }
        private ColorCategory _color = ColorCategory.Neutral;

        public string AttachementFile
        {
            get { return _attachementFile; }
            set
            {
                Set(ref _attachementFile, value);
                RaisePropertyChanged("HasAttachement");
                RaisePropertyChanged("AttachementImage");
            }
        }
        private string _attachementFile;

        public NoteItem()
        {
            Id = GenerateGuid();
        }

        public NoteItem(string title, string content)
            : this()
        {
            Title = title;
            Content = content;
        }

        /// <summary>
        /// Generates a trimmed guid
        /// </summary>
        /// <remarks>
        /// We do not use the full guid, because the Tag length of a notification is limited.
        /// </remarks>
        /// <returns></returns>
        private string GenerateGuid()
        {
            // 598723c4-fa3e-448e-b3c0-5e43d389ac25 ==> 598723c4fa3e448e
            var guid = Guid.NewGuid().ToString();
            guid = guid.Substring(0, 18);
            guid = guid.Replace("-", "");
            return guid;
        }

        public NoteItem Clone()
        {
            var clone = new NoteItem();
            clone.Id = Id;
            clone.Title = Title;
            clone.Content = Content;
            clone.Color = Color;
            clone.AttachementFile = AttachementFile;
            return clone;
        }

        public bool HasAttachement
        {
            get { return AttachementFile != null; }
        }

        public ImageSource AttachementImage
        {
            get
            {
                if (!HasAttachement)
                    return null;

                var path = string.Format("{0}/{1}", IOConstants.APPDATA_LOCAL_SCHEME, AppConstants.ATTACHEMENT_BASE_PATH + AttachementFile);
                return new BitmapImage(new Uri(path, UriKind.Absolute));
            }
        }

        public bool IsEmtpy
        {
            get
            {
                return string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Content) && !HasAttachement;
            }
        }
    }
}
