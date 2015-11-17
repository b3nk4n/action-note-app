﻿using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using Ninject;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Notifications;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Service class to manage the action center.
    /// </summary>
    public class ToastUpdateService : IToastUpdateService
    {
        public const string GROUP_NOTE = "note";
        public const string GROUP_QUICK_NOTE = "quickNote";

        private Localizer _localizer = new Localizer("ActionNote.Common");

        private IToastService _toastService;

        [Inject]
        public ToastUpdateService(IToastService toastService)
        {
            _toastService = toastService;
        }

        public async void Refresh(INotesRepository notesRepository)
        {
            _toastService.ClearHistory();

            var allNotes = notesRepository.GetAll();

            var sorted = NoteUtils.Sort(allNotes, AppSettings.SortNoteBy.Value);

            for (int i = allNotes.Count - 1; i >= 0; --i)
            {
                var note = allNotes[i];

                if (i != 0)
                    await Task.Delay(100);

                var toastModel = GetToastModel(note);
                var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
                toastNotification.SuppressPopup = true;
                toastNotification.Group = GROUP_NOTE;
                toastNotification.Tag = note.Id; // just to find the notificication within this service
                _toastService.Show(toastNotification);
            }

            await Task.Delay(100);

            if (AppSettings.QuickNotesEnabled.Value)
            {
                AddQuickNotes();
            }
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

        public void DeleteNotesThatAreMissingInActionCenter(INotesRepository notesRepository)
        {
            // find IDs to remove
            var noteIdsToRemove = notesRepository.GetAllIds();
            foreach (var historyItem in _toastService.History)
            {
                noteIdsToRemove.Remove(historyItem.Tag);
            }

            // remove from repository
            foreach (var noteId in noteIdsToRemove)
            {
                notesRepository.Remove(noteId);
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
                Actions = new AdaptiveActions()
                {
                    Children =
                    {
                        new AdaptiveAction()
                        {
                            ActivationType = ToastActivationType.Foreground,
                            Content = "",
                            Arguments = "edit-" + noteItem.Id,
                            ImageUri = "Assets/Images/edit.png"
                        },
                        new AdaptiveAction()
                        {
                            ActivationType = ToastActivationType.Background,
                            Content = "",
                            Arguments = "delete-" + noteItem.Id,
                            ImageUri = "Assets/Images/delete.png"
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

            return toastModel;
        }
    }
}
