using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionTriggeredBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;

        private Localizer _localizer = new Localizer("ActionNote.Common");

        private static Mutex backgroundMutex = new Mutex(false, "createNoteBeforeDiff");

        private static volatile bool hasMutex;

        public ActionTriggeredBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            Logger.WriteLine("ActionTriggeredTask: started");

            taskInstance.Canceled += (s, e) =>
            {
                if (hasMutex)
                    backgroundMutex.ReleaseMutex();
            };

            var start = DateTimeOffset.Now.Ticks;

            hasMutex = backgroundMutex.WaitOne(3000); // wait time is generally not needed, but just to make sure we are not blocking the save process ... (close your eyes and have a dirty try!)   

            var end = DateTimeOffset.Now.Ticks;

            Logger.WriteLine("ActionTriggeredTask: entered (waited: {0})", end - start);

            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details != null &&
                AppSettings.ShowNotesInActionCenter.Value)
            {
                if (details.Argument == "quickNote")
                {
                    if (details.UserInput.ContainsKey("content"))
                    {
                        string content = null;
                        string title = null;
                        var input = details.UserInput["content"] as string;

                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            var quickNotesDefaultTitle = AppSettings.QuickNotesDefaultTitle.Value;
                            if (string.IsNullOrEmpty(quickNotesDefaultTitle))
                            {
                                quickNotesDefaultTitle = _localizer.Get("QuickNotes");
                            }

                            if (AppSettings.QuickNotesContentType.Value == AppConstants.QUICK_NOTES_TITLE_AND_CONTENT)
                            {
                                var splitIndex = input.IndexOf("\r");

                                if (splitIndex != -1)
                                {
                                    title = input.Substring(0, splitIndex).Trim();
                                    content = input.Substring(splitIndex + 1, input.Length - splitIndex - 1).Trim();
                                }
                                else
                                {
                                    content = input.Trim();
                                }

                                if (string.IsNullOrWhiteSpace(content))
                                {
                                    content = title;
                                    title = quickNotesDefaultTitle;
                                }
                                else if (string.IsNullOrWhiteSpace(title))
                                {
                                    title = quickNotesDefaultTitle;
                                }
                            }
                            else
                            {
                                title = quickNotesDefaultTitle;
                                content = input;
                            }

                            if (!string.IsNullOrWhiteSpace(title) ||
                                !string.IsNullOrWhiteSpace(content))
                            {
                                var noteItem = new NoteItem(title, content);
                                _dataService.FlagNotesNeedReload();
                                _dataService.AddNoteAsync(noteItem).Wait();

                                if (AppSettings.SortNoteInActionCenterBy.Value == AppConstants.SORT_DATE)
                                {
                                    // add it into the action center at the beginning when we order for date.
                                    _actionCenterService.AddToTop(noteItem).Wait();
                                }
                                else
                                {
                                    // refresh all, because new note could be not at the top of the list
                                    var getAllTask = _dataService.GetAllNotes();
                                    getAllTask.Wait();
                                    var notes = getAllTask.Result;
                                    if (notes != null)
                                        _actionCenterService.RefreshAsync(notes).Wait();
                                }
                            }
                        }
                    }
                }
            }

            if (hasMutex)
                backgroundMutex.ReleaseMutex();

            Logger.WriteLine("ActionTriggeredTask: DONE");

            deferral.Complete();
        }
    }
}
