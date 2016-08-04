using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using Ninject;
using System.Linq;
using UWPCore.Framework.Common;
using UWPCore.Framework.Notifications;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;
using System;
using System.Collections.Generic;
using UWPCore.Framework.Devices;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Service class to manage the action center.
    /// </summary>
    public class ActionCenterService : IActionCenterService
    {
        public static StoredObjectBase<DateTimeOffset> BackgroundTaskToastRemoveBlockingUntil = new LocalObject<DateTimeOffset>("toastBlocking", DateTimeOffset.MinValue);

        // refesh block for FileOpenPicker
        public static StoredObjectBase<DateTimeOffset> RefreshBlockingUntil = new LocalObject<DateTimeOffset>("refreshBlocking", DateTimeOffset.MinValue);

        private const int MAX_NOTIFICATOINS = 19;

        /// <summary>
        /// Timed refresh Action Center workaround (Expiration after 72h bug)
        /// <see cref="https://social.msdn.microsoft.com/Forums/Windowsapps/en-US/e3d9962d-c11a-403a-8d25-eec30fcdd7b6/defaultmaximum-expirationtime-for-toastnotification?forum=wpdevelop"/>
        /// </summary>
        /// <remarks>
        /// The default value is MAX date time, bacause everything should not start before the app itself did at least one refresh of the action center.
        /// </remarks>
        public static StoredObjectBase<DateTimeOffset> LastActionCenterRefresh = new LocalObject<DateTimeOffset>("lastExpireRefresh", DateTimeOffset.MaxValue);

        public const string GROUP_NOTE = "note";
        public const string GROUP_QUICK_NOTE = "quickNote";

        private Localizer _localizer = new Localizer("ActionNote.Common");

        private IToastService _toastService;
        private IDeviceInfoService _deviceInfoService;

        [Inject]
        public ActionCenterService(IToastService toastService, IDeviceInfoService deviceInfoService)
        {
            _toastService = toastService;
            _deviceInfoService = deviceInfoService;
        }

        public void AddToTop(NoteItem noteItem)
        {
            var containsQuickNotes = ContainsQuickNotes();

            if (containsQuickNotes)
                RemoveQuickNotes();

            AddNotification(noteItem);

            if (containsQuickNotes)
            {
                AddQuickNotes();
            }  
        }

        public void Clear()
        {
            _toastService.ClearHistory();
        }

        public async Task Refresh(IList<NoteItem> noteItems)
        {
            if (IsRefreshBlocked())
                return;

            LastActionCenterRefresh.Value = DateTimeOffset.Now;

            _toastService.ClearHistory();

            if (AppSettings.ShowNotesInActionCenter.Value)
            {
                var sorted = NoteUtils.Sort(noteItems, AppSettings.SortNoteInActionCenterBy.Value).Reverse().ToList();

                for (int i = 0; i < Math.Min(sorted.Count, MAX_NOTIFICATOINS); ++i)
                {
                    var note = sorted[i];

                    AddNotification(note);
                }
            }

            if (AppSettings.QuickNotesEnabled.Value)
            {
                await Task.Delay(100);
                AddQuickNotes();
            }
        }

        private void AddNotification(NoteItem note)
        {
            if (note.IsHidden)
                return;

            var toastModel = GetToastModel(note);
            var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
            toastNotification.SuppressPopup = true;
            toastNotification.Group = GROUP_NOTE;
            toastNotification.Tag = note.ShortId; // just to find the notificication within this service. Shortid because it has a limited size
            toastNotification.ExpirationTime = DateTimeOffset.Now.AddDays(31); // Does not solve anything, it is still 72h !!!
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
            quickNoteToastNotification.SuppressPopup = true; // REMEMBER: suprress-popup currently can causes a bug that the "reference" to the notification can get lost after remove/replace => sometimes ContainsQuickNotes() even when it was deleted
            quickNoteToastNotification.Group = GROUP_QUICK_NOTE;
            quickNoteToastNotification.Tag = "quickNote";
            quickNoteToastNotification.ExpirationTime = DateTimeOffset.Now.AddDays(31); // Does not solve anything, it is still 72h !!!
            _toastService.Show(quickNoteToastNotification);
        }

        public void RemoveQuickNotes()
        {
            _toastService.RemoveGroupeFromHistory(GROUP_QUICK_NOTE);
        }

        public IList<NoteItem> DiffWithNotesInActionCenter(IList<NoteItem> noteItems)
        {
            var notesToRemove = new List<NoteItem>();
            foreach (var item in noteItems)
            {
                var toastItemsInHistory = _toastService.GetByTagFromHistory(item.ShortId);
                if (toastItemsInHistory == null || toastItemsInHistory.Count() == 0)
                    notesToRemove.Add(item);
            }

            return notesToRemove;
        }

        public void StartTemporaryRemoveBlocking(int seconds)
        {
            BackgroundTaskToastRemoveBlockingUntil.Value = DateTimeOffset.Now.AddSeconds(seconds);
        }

        public bool IsRemoveBlocked()
        {
            return BackgroundTaskToastRemoveBlockingUntil.Value > DateTimeOffset.Now;
        }

        public void StartTemporaryRefreshBlocking(int seconds)
        {
            RefreshBlockingUntil.Value = DateTimeOffset.Now.AddSeconds(seconds);
        }

        public bool IsRefreshBlocked()
        {
            return RefreshBlockingUntil.Value > DateTimeOffset.Now;
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
                                }
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
                                new AdaptiveText()
                                {
                                    Content = _localizer.Get("QuickNotesDescription"),
                                    HintStyle = TextStyle.Base,
                                    HintWrap = true
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
                        new AdaptiveInput()
                        {
                            Type = InputType.Text,
                            PlaceHolderContent = "one more?",
                            Id = "content2",
                        },
                        new AdaptiveAction()
                        {
                            ActivationType = ToastActivationType.Background,
                            Content = _localizer.Get("Save.Label"),
                            //HintInputId = "content2",
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

            if (AppSettings.QuickNotesContentType.Value == AppConstants.QUICK_NOTES_TITLE_AND_CONTENT)
            {
                toastModel.Actions = new AdaptiveActions()
                {
                    Children =
                    {
                        new AdaptiveInput()
                        {
                            Type = InputType.Text,
                            PlaceHolderContent = _localizer.Get("NoteTitle.PlaceholderText"),
                            Id = "title",
                        },
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
                            Arguments = "quickNote",
                            ImageUri = "Assets/Images/save.png"
                        }
                    }
                };
            }
            else
            {
                toastModel.Actions = new AdaptiveActions()
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
                };
            }

            return toastModel;
        }

        /// <summary>
        /// Gets the full ID of a short ID by expection everything is unique.
        /// Workaround is needed because Action Center does not support full ID lengths.
        /// </summary>
        /// <param name="ids">The ID list.</param>
        /// <param name="shortId">The short ID to find.</param>
        /// <returns></returns>
        public static string GetIdFromShortId(IList<string> ids, string shortId)
        {
            if (ids == null)
                return null;

            foreach (var id in ids)
            {
                if (id.StartsWith(shortId))
                    return id;
            }

            return null;
        }
    }
}
