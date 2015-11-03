using ActionNote.Common.Converters;
using ActionNote.Common.Models;
using Ninject;
using System;
using System.Collections.Generic;
using System.Text;
using UWPCore.Framework.Notifications;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Services
{
    public class ToastUpdateService : IToastUpdateService
    {
        public const string GROUP_NOTE = "note";

        private IToastService _toastService;

        [Inject]
        public ToastUpdateService(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void Refresh(INotesRepository notesRepository)
        {
            _toastService.ClearHistory();

            foreach (var note in notesRepository.GetAll())
            {
                var toastModel = GetToastModel(note);
                var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
                toastNotification.SuppressPopup = true;
                toastNotification.Group = GROUP_NOTE;
                toastNotification.Tag = note.Id;
                
                _toastService.Show(toastNotification);
            }
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
                ActivationType = ToastActivationType.Background,
                Scenario = ToastScenario.Reminder,
                Launch = noteItem.Id,
                Visual = new AdaptiveVisual()
                {
                    Branding = VisualBranding.None,
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
                                    Source = (string)new ColorCategoryToImageConverter().Convert(noteItem.Color, null, null, null)
                                },
                                new AdaptiveText()
                                {
                                    Content = noteItem.Title,
                                    HintStyle = TextStyle.Title,
                                    HintMaxLines = 2,
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
                            Content = "Edit",
                            Arguments = "edit",
                        },
                        new AdaptiveAction()
                        {
                            ActivationType = ToastActivationType.Background,
                            Content = "Delete",
                            Arguments = "delete",
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
            }

            return toastModel;
        }
    }
}
