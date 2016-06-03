using ActionNote.Common;
using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Threading;
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
        private ITilePinService _tilePinService;

        private Localizer _localizer = new Localizer("ActionNote.Common");

        private static Mutex backgroundMutex = new Mutex(false, "createNoteBeforeDiff");

        private static volatile bool hasMutex;

        public ActionTriggeredBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
            _tilePinService = injector.Get<ITilePinService>();
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
            if (details != null)
            {
                if (details.Argument == "quickNote")
                {
                    string title = "";
                    string content = "";
                    
                    if (details.UserInput.ContainsKey("title")) 
                        title = details.UserInput["title"] as string;
                    if (details.UserInput.ContainsKey("content"))
                        content = details.UserInput["content"] as string;

                    if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(content))
                    {
                        // process title
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            title = GetDefaultTitle();
                        }
                        else
                        {
                            // single-linify title (\r\n for PC, \r for Mobile)
                            title = title.Replace("\r\n", " ").Replace("\r", " ").Trim();
                        }

                        // process content
                        content = content.Trim();

                        // store note according to sorting
                        var noteItem = new NoteItem(title, content);
                        noteItem.Color = ColorCategoryConverter.FromAnyString(AppSettings.DefaultNoteColor.Value);

                        // load notes
                        var getAllTask = _dataService.GetAllNotes();
                        getAllTask.Wait();
                        var notes = getAllTask.Result;
                        notes.Add(noteItem);

                        if (AppSettings.SortNoteInActionCenterBy.Value == AppConstants.SORT_DATE &&
                            AppSettings.ShowNotesInActionCenter.Value)
                        {
                            // add it into the action center at the beginning when we order for date.
                            _actionCenterService.AddToTop(noteItem);//.Wait();
                        }
                        else
                        {
                            // refresh all, because new note could be not at the top of the list
                            if (notes != null)
                                _actionCenterService.Refresh(notes).Wait();
                        }

                        // add note physically after adding the notification
                        // 1) for faster response time
                        // 2) because an added note might be deleted, when the app is launched and not pushed as a notification
                        _dataService.FlagNotesNeedReload();
                        _dataService.AddNoteAsync(noteItem).Wait();

                        _tilePinService.UpdateMainTile(notes);
                    }
                }
            }

            if (hasMutex)
                backgroundMutex.ReleaseMutex();

            Logger.WriteLine("ActionTriggeredTask: DONE");

            deferral.Complete();
        }

        /// <summary>
        /// Gets the default title of a note.
        /// </summary>
        /// <returns>Returns the default title.</returns>
        private string GetDefaultTitle()
        {
            var quickNotesDefaultTitle = AppSettings.QuickNotesDefaultTitle.Value;
            if (string.IsNullOrEmpty(quickNotesDefaultTitle))
            {
                quickNotesDefaultTitle = _localizer.Get("QuickNote");
            }
            return quickNotesDefaultTitle;
        }
    }
}
