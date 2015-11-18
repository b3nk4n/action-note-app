using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using Ninject;
using System.Threading.Tasks;
using System.Linq;
using UWPCore.Framework.Common;
using UWPCore.Framework.Notifications;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;
using System;
using System.Collections.Generic;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Service class to manage the action center.
    /// </summary>
    public class ActionCenterService : IActionCenterService
    {
        public static StoredObjectBase<DateTimeOffset> BackgroundTaskToastRemoveBlockingUntil = new LocalObject<DateTimeOffset>("toastBlocking", DateTimeOffset.MinValue);

        public const string GROUP_NOTE = "note";
        public const string GROUP_QUICK_NOTE = "quickNote";

        private Localizer _localizer = new Localizer("ActionNote.Common");

        private IToastService _toastService;

        [Inject]
        public ActionCenterService(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void AddToTop(NoteItem noteItem)
        {
            var containsQuickNotes = ContainsQuickNotes();

            if (containsQuickNotes)
                RemoveQuickNotes();

            AddNotification(noteItem);

            if (containsQuickNotes)
                AddQuickNotes();
        }

        public void Clear()
        {
            _toastService.ClearHistory();
        }

        public async Task RefreshAsync(IList<NoteItem> noteItems)
        {
            _toastService.ClearHistory();

            var sorted = NoteUtils.Sort(noteItems, AppSettings.SortNoteInActionCenterBy.Value).Reverse().ToList();

            for (int i = 0; i < sorted.Count; ++i)
            {
                var note = sorted[i];

                if (i != 0)
                    await Task.Delay(10);

                AddNotification(note);
            }

            if (AppSettings.QuickNotesEnabled.Value)
            {
                await Task.Delay(10);

                AddQuickNotes();
            }
        }

        private void AddNotification(NoteItem note)
        {
            var toastModel = GetToastModel(note);
            var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
            toastNotification.SuppressPopup = true;
            toastNotification.Group = GROUP_NOTE;
            toastNotification.Tag = note.Id; // just to find the notificication within this service
            _toastService.Show(toastNotification);
        }

        public bool ContainsQuickNotes()
        {
            return _toastService.GetByGroupFromHistory(GROUP_QUICK_NOTE).Count() > 0;
        }

        public void AddQuickNotes()
        {
            // add the quick note toast
            var quickNoteToastModel = GetQuickNoteToastModel();
            var quickNoteToastNotification = _toastService.AdaptiveFactory.Create(quickNoteToastModel);
            quickNoteToastNotification.SuppressPopup = true;
            quickNoteToastNotification.Group = GROUP_QUICK_NOTE;
            quickNoteToastNotification.Tag = "quickNote"; // just to find the notificication within this service
            _toastService.Show(quickNoteToastNotification);
        }

        public void RemoveQuickNotes()
        {
            _toastService.RemoveGroupeFromHistory(GROUP_QUICK_NOTE);
        }

        public IList<string> DiffWithNotesInActionCenter(IList<NoteItem> noteItems)
        {
            // find IDs to remove
            var idsToRemove = new List<string>();
            foreach (var item in noteItems)
            {
                var toastItemsInHistory = _toastService.GetByTagFromHistory(item.Id);
                if (toastItemsInHistory == null || toastItemsInHistory.Count() == 0)
                    idsToRemove.Add(item.Id);
            }

            return idsToRemove;
        }

        public void StartTemporaryRemoveBlocking(int seconds)
        {
            BackgroundTaskToastRemoveBlockingUntil.Value = DateTimeOffset.Now.AddSeconds(seconds);
        }

        public bool IsRemoveBlocked()
        {
            return BackgroundTaskToastRemoveBlockingUntil.Value > DateTimeOffset.Now;
        }

        public int NotesCount
        {
            get
            {
                return _toastService.GetByGroupFromHistory(GROUP_NOTE).Count();
            }
        }

        private AdaptiveToastModel GetToastModel(NoteItem noteItem)
        {
            var toastModel = new AdaptiveToastModel()
            {
                ActivationType = ToastActivationType.Foreground,
                Launch = noteItem.Id, // when clicked on it
                Visual = new AdaptiveVisual()
                {
                    Bindings =
                    {
                        new AdaptiveBinding()
                        {
                            Template = VisualTemplate.ToastGeneric,
                            Children =
                            {
                                new AdaptiveImage()
                                {
                                    Placement = ImagePlacement.AppLogoOverride,
                                    HintCrop = ImageHintCrop.Circle,
                                    Source = noteItem.GetIconImagePath()
                                },
                                new AdaptiveText()
                                {
                                    Content = noteItem.Title,
                                    HintStyle = TextStyle.Title,
                                    HintWrap = true
                                },
                                new AdaptiveText()
                                {
                                    Content = noteItem.Content,
                                    HintStyle = TextStyle.Base,
                                    HintWrap = true
                                },

                            }
                        }
                    }
                },
                Audio = new AdaptiveAudio()
                {
                    Silent = true,
                }
            };

            if (noteItem.HasAttachement)
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                toastModel.Visual.Bindings[0].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Inline,
                    Source = picturePath
                });

                // change szenario that the image is bigger
                toastModel.Scenario = ToastScenario.Reminder;
            }

            return toastModel;
        }

        private AdaptiveToastModel GetQuickNoteToastModel()
        {
            var toastModel = new AdaptiveToastModel()
            {
                ActivationType = ToastActivationType.Foreground,
                Launch = "quickNote", // when clicked on it, open app to add a new note
                Visual = new AdaptiveVisual()
                {
                    Bindings =
                    {
                        new AdaptiveBinding()
                        {
                            Template = VisualTemplate.ToastGeneric,
                            Children =
                            {
                                new AdaptiveImage()
                                {
                                    Placement = ImagePlacement.AppLogoOverride,
                                    Source = "Assets/StoreLogo.png"
                                },
                                new AdaptiveText()
                                {
                                    Content = _localizer.Get("QuickNotes"),
                                    HintStyle = TextStyle.Title
                                },
                            }
                        }
                    }
                },
                Actions = new AdaptiveActions()
                {
                    Children =
                    {
                        new AdaptiveInput()
                        {
                            Type = InputType.Text,
                            PlaceHolderContent = _localizer.Get("NoteContent.PlaceholderText"),
                            Id = "content",
                        },
                        new AdaptiveAction()
                        {
                            ActivationType = ToastActivationType.Background,
                            Content = _localizer.Get("Save.Label"),
                            HintInputId = "content",
                            Arguments = "quickNote",
                            ImageUri = "Assets/Images/save.png"
                        }
                    }
                },
                Audio = new AdaptiveAudio()
                {
                    Silent = true,
                }
            };

            string contentText;
            if (AppSettings.QuickNotesContentType.Value == AppConstants.QUICK_NOTES_TITLE_AND_CONTENT)
                contentText = string.Format("{0}\r\n{1}", _localizer.Get("NoteTitle.PlaceholderText"), _localizer.Get("NoteContent.PlaceholderText"));
            else
                contentText = _localizer.Get("NoteContent.PlaceholderText");

            (toastModel.Actions.Children[0] as AdaptiveInput).PlaceHolderContent = contentText;

            return toastModel;
        }
    }
}
