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
        Orange,
        Violett,
        [DefaultValue(true)]
        Neutral
    }

    [DataContract]
    public class NoteItem : BindableBase, IRepositoryItem<string>
    {
        private bool _hasContentChanged = false;
        private bool _hasAttachementChanged = false;

        [DataMember(Name = "_id")]
        public string Id {
            get { return _id; }
            set { Set(ref _id, value); }
        }
        private string _id;

        /// <summary>
        /// Short ID is used internally to 
        /// </summary>
        [IgnoreDataMember]
        public string ShortId
        {
            get { return _id.Substring(0, 16); }
        }

        [DataMember(Name = "title")]
        public string Title {
            get { return _title; }
            set {
                Set(ref _title, value);
                _hasContentChanged = true;
            }
        }
        private string _title;

        [DataMember(Name = "content")]
        public string Content {
            get { return _content; }
            set {
                Set(ref _content, value);
                _hasContentChanged = true;
            }
        }
        private string _content;

        [DataMember(Name = "color")]
        public ColorCategory Color {
            get { return _color; }
            set {
                Set(ref _color, value);
                _hasContentChanged = true;
            }
        }
        private ColorCategory _color = ColorCategory.Neutral;

        [DataMember(Name = "flag")]
        public bool IsImportant
        {
            get { return _isImportant; }
            set {
                Set(ref _isImportant, value);
                _hasContentChanged = true;
            }
        }
        private bool _isImportant;

        [DataMember(Name = "date")]
        public DateTimeOffset ChangedDate
        {
            get { return _changedDate; }
            set {
                Set(ref _changedDate, value);
            }
        }
        private DateTimeOffset _changedDate;

        [DataMember(Name = "file")]
        public string AttachementFile
        {
            get { return _attachementFile; }
            set
            {
                Set(ref _attachementFile, value);
                _hasAttachementChanged = true;
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
            // 598723c4-fa3e-448e-b3c0-5e43d389ac25 ==> 598723c4fa3e448eb3c05e43 (24 digits for mongodb)
            var guid = Guid.NewGuid().ToString();
            guid = guid.Substring(0, 28);
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
            clone.MarkAsUnchanged();
            return clone;
        }

        [IgnoreDataMember]
        public bool HasAttachement
        {
            get { return AttachementFile != null; }
        }

        [IgnoreDataMember]
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

        [IgnoreDataMember]
        public bool IsEmtpy
        {
            get
            {
                return string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Content) && !HasAttachement;
            }
        }

        [IgnoreDataMember]
        public bool HasContentChanged
        {
            get
            {
                return _hasContentChanged;
            }
        }

        [IgnoreDataMember]
        public bool HasAttachementChanged
        {
            get
            {
                return _hasAttachementChanged;
            }
        }

        public void MarkAsUnchanged()
        {
            _hasContentChanged = false;
            _hasAttachementChanged = false;
        }
    }
}
