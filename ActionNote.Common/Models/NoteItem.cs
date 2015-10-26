using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Mvvm;

namespace ActionNote.Common.Models
{
    public class NoteItem : BindableBase, IRepositoryItem<string>
    {
        public string Id { get { return _id; } set { Set(ref _id, value); } }
        private string _id;

        public string Title { get { return _title; } set { Set(ref _title, value); } }
        private string _title;

        public string Content { get { return _content; } set { Set(ref _content, value); } }
        private string _content;

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
    }
}
