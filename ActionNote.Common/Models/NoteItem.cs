using System;
using System.ComponentModel;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using UWPCore.Framework.Common;
using System.Runtime.Serialization;

namespace ActionNote.Common.Models
{
    public enum ColorCategory
    {
        Red,
        Blue,
        Green,
        Yellow,
        Violett,
        [DefaultValue(true)]
        Neutral
    }

    [DataContract]
    public class NoteItem : BindableBase, IRepositoryItem<string>
    {
        [DataMember(Name = "_id")]
        public string Id {
            get { return _id; }
            set { Set(ref _id, value); }
        }
        private string _id;

        [DataMember(Name = "title")]
        public string Title {
            get { return _title; }
            set { Set(ref _title, value); }
        }
        private string _title;

        [DataMember(Name = "content")]
        public string Content {
            get { return _content; }
            set { Set(ref _content, value); }
        }
        private string _content;

        [DataMember(Name = "color")]
        public ColorCategory Color {
            get { return _color; }
            set { Set(ref _color, value);  }
        }
        private ColorCategory _color = ColorCategory.Neutral;

        [DataMember(Name = "flag")]
        public bool IsImportant
        {
            get { return _isImportant; }
            set { Set(ref _isImportant, value); }
        }
        private bool _isImportant;

        [DataMember(Name = "date")]
        public DateTimeOffset ChangedDate
        {
            get { return _changedDate; }
            set { Set(ref _changedDate, value); }
        }
        private DateTimeOffset _changedDate;

        [DataMember(Name = "file")]
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

        public string GetIconImagePath()
        {
            return string.Format("/Assets/Images/{0}{1}{2}.png", 
                Color.ToString().FirstLetterToLower(), 
                (HasAttachement ? "_att" : string.Empty),
                (IsImportant ? "_flag" : string.Empty));
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
            //var guid = Guid.NewGuid().ToString();
            //guid = guid.Substring(0, 18);
            //guid = guid.Replace("-", "");
            //return guid;
            // trimmed, because schedules tag is shorter than toast-id :(
            var guid = Guid.NewGuid().ToString();
            guid = guid.Substring(0, 13);
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
            clone.IsImportant = IsImportant;
            clone.ChangedDate = ChangedDate;
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
