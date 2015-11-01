using ActionNote.Common.Converters;
using ActionNote.Common.Models;
using Ninject;
using System;
using System.Collections.Generic;
using System.Text;
using UWPCore.Framework.Notifications;
using UWPCore.Framework.Notifications.Models;

namespace ActionNote.Common.Services
{
    public class ToastUpdateService : IToastUpdateService
    {
        public const string GROUP_NOTE = "note";

        private IToastService _toastService;
        private INotesRepository _notesRepository;

        [Inject]
        public ToastUpdateService(IToastService toastService, INotesRepository notesRepository)
        {
            _toastService = toastService;
            _notesRepository = notesRepository;

            // ensure the repository has been loaded
            _notesRepository.Load(); // TODO: lazy load?
        }

        public void Refresh()
        {
            _toastService.ClearHistory();

            foreach (var note in _notesRepository.GetAll())
            {
                var toastModel = GetToastModel(note);
                var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
                toastNotification.SuppressPopup = true;
                toastNotification.Group = GROUP_NOTE;
                toastNotification.Tag = note.Id;
                
                _toastService.Show(toastNotification);
            }

            //var toastModel = GetContentToast("Content Note", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
            //var toastNotification = _toastService.AdaptiveFactory.Create(toastModel);
            //toastNotification.SuppressPopup = true;
            //_toastService.Show(toastNotification);

            //var toastModel2 = GetListToast("List Note", new List<string>() { "First item", "Second item", "Third item", "Fourth item", "Fifth item" });
            //var toastNotification2 = _toastService.AdaptiveFactory.Create(toastModel2);
            //toastNotification2.SuppressPopup = true;
            //_toastService.Show(toastNotification2);

            //var toastModel3 = GetPictureToast("Picture Note", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", "/Assets/Square150x150Logo.scale-200.png");
            //var toastNotification3 = _toastService.AdaptiveFactory.Create(toastModel3);
            //toastNotification3.SuppressPopup = true;
            //_toastService.Show(toastNotification3);
        }

        public void DeleteNotesThatAreMissingInActionCenter()
        {
            // find IDs to remove
            var noteIdsToRemove = GetAllNoteIds();
            foreach (var historyItem in _toastService.History)
            {
                noteIdsToRemove.Remove(historyItem.Tag);
            }

            // remove from repository
            foreach (var noteId in noteIdsToRemove)
            {
                _notesRepository.Remove(noteId);
            }
        }

        private IList<string> GetAllNoteIds()
        {
            var idList = new List<string>();

            foreach (var item in _notesRepository.GetAll())
            {
                idList.Add(item.Id);
            }

            return idList;
        }

        private AdaptiveToastModel GetToastModel(NoteItem noteItem)
        {
            return new AdaptiveToastModel()
            {
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
        }

        //private AdaptiveToastModel GetListToast(string title, IList<string> items)
        //{
        //    var sb = new StringBuilder();

        //    foreach (var item in items)
        //    {
        //        sb.AppendLine("▪ " + item);
        //    }

        //    return new AdaptiveToastModel()
        //    {
        //        Visual = new AdaptiveVisual()
        //        {
        //            Branding = VisualBranding.None,
        //            Bindings =
        //            {
        //                new AdaptiveBinding()
        //                {
        //                    Template = VisualTemplate.ToastGeneric,
        //                    Children =
        //                    {
        //                        new AdaptiveText()
        //                        {
        //                            Content = title,
        //                            HintStyle = TextStyle.Title,
        //                            HintMaxLines = 2,
        //                            HintWrap = true
        //                        },
        //                        new AdaptiveText()
        //                        {
        //                            Content = sb.ToString(),
        //                            HintStyle = TextStyle.Base,
        //                            HintWrap = true
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    };
        //}

        //private AdaptiveToastModel GetPictureToast(string title, string content, string picturePath)
        //{
        //    return new AdaptiveToastModel()
        //    {
        //        Visual = new AdaptiveVisual()
        //        {
        //            Branding = VisualBranding.None,
        //            Bindings =
        //            {
        //                new AdaptiveBinding()
        //                {
        //                    Template = VisualTemplate.ToastGeneric,
        //                    Children =
        //                    {
        //                        new AdaptiveText()
        //                        {
        //                            Content = title,
        //                            HintStyle = TextStyle.Title,
        //                            HintMaxLines = 2,
        //                            HintWrap = true
        //                        },
        //                        new AdaptiveText()
        //                        {
        //                            Content = content,
        //                            HintStyle = TextStyle.Base,
        //                            HintWrap = true
        //                        },
        //                        new AdaptiveImage()
        //                        {
        //                            HintAlign = ImageHintAlign.Stretch,
        //                            Placement = ImagePlacement.Inline,
        //                            Source = picturePath
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    };
        //}
    }
}
