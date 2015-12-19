using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using UWPCore.Framework.Notifications;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ITilePinService _tilePinService;
        private IBadgeService _badgeService;

        private static Mutex backgroundMutex = new Mutex(false, "createNoteBeforeDiff");

        private static volatile bool hasMutex;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
            _tilePinService = injector.Get<ITilePinService>();
            _badgeService = injector.Get<IBadgeService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            Logger.WriteLine("ActionBarChangedTask: started");

            taskInstance.Canceled += (s, e) =>
            {
                if (hasMutex)
                    backgroundMutex.ReleaseMutex();
            };

            // wait to ensure ActionTriggeredBackgroundTask is running first
            await Task.Delay(1000);

            var start = DateTimeOffset.Now.Ticks;

            hasMutex = backgroundMutex.WaitOne();

            var end = DateTimeOffset.Now.Ticks;

            Logger.WriteLine("ActionBarChangedTask: entered (waited: {0})", end - start);

            // exit when we didn't get the mutex
            if (!hasMutex)
            {
                deferral.Complete();
                return;
            }

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;

            if (details != null &&
                !_actionCenterService.IsRemoveBlocked() &&
                AppSettings.ShowNotesInActionCenter.Value &&
                details.ChangeType == ToastHistoryChangedType.Removed) // Remark: ToastHistoryChangedType.Cleared seems not to be supported up to now?
            {
                // load data
                _dataService.LoadNotesAsync().Wait(); // load it manually here, because of _dataService.NotesCount

                if (AppSettings.AllowRemoveNotes.Value)
                {
                    var getAllTask = _dataService.GetAllNotes();
                    getAllTask.Wait();
                    var notes = getAllTask.Result;

                    if (notes != null)
                    {
                        var diff = _actionCenterService.DiffWithNotesInActionCenter(notes);

                        Logger.WriteLine("ActionBarChangedTask: delete {0} notes", diff.Count);

                        if (diff.Count > 0)
                        {
                            // unpin deleted notes
                            foreach (var note in diff)
                            {
                                _tilePinService.UnpinAsync(note.Id).Wait();
                            }

                            _dataService.FlagNotesNeedReload();
                            _dataService.FlagArchiveNeedsReload();
                            _dataService.MoveRangeToArchiveAsync(diff).Wait();
                        }

                        if (AppSettings.QuickNotesEnabled.Value &&
                            !_actionCenterService.ContainsQuickNotes())
                        {
                            // add quick notes when it was removed by klicking on it or using it.
                            _actionCenterService.AddQuickNotes();
                        }

                        var badge = _badgeService.Factory.CreateBadgeNumber(_dataService.NotesCount);
                        _badgeService.GetBadgeUpdaterForApplication().Update(badge);
                    }
                }
                else
                {
                    // only refresh when there has been a change (because the bgtask is called multiple times when we do multiple remove operations)
                    if (_actionCenterService.NotesCount == _dataService.NotesCount)
                    {
                        // when only the quick notes was removed, just re-add it
                        if (AppSettings.QuickNotesEnabled.Value && !_actionCenterService.ContainsQuickNotes())
                        {
                            _actionCenterService.AddQuickNotes();
                        }
                    }
                    else
                    {
                        var getAllTask = _dataService.GetAllNotes();
                        getAllTask.Wait();
                        var notes = getAllTask.Result;

                        if (notes != null)
                            _actionCenterService.RefreshAsync(notes).Wait();
                    }
                }
            }

            if (hasMutex)
                backgroundMutex.ReleaseMutex();

            Logger.WriteLine("ActionBarChangedTask: DONE");

            deferral.Complete();
        }
    }
}
