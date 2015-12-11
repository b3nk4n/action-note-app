using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System.Threading;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ITilePinService _tilePinService;

        private static Mutex backgroundMutex = new Mutex(false, "createNoteBeforeDiff");

        private static volatile bool hasMutex;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
            _tilePinService = injector.Get<ITilePinService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            taskInstance.Canceled += (s, e) =>
            {
                if (hasMutex)
                    backgroundMutex.ReleaseMutex();
            };

            // wait to ensure ActionTriggeredBackgroundTask is running first
            await Task.Delay(1000);

            hasMutex = backgroundMutex.WaitOne();
            

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;

            if (details != null &&
                !_actionCenterService.IsRemoveBlocked() &&
                AppSettings.ShowNotesInActionCenter.Value &&
                details.ChangeType == ToastHistoryChangedType.Removed) // Remark: ToastHistoryChangedType.Cleared seems not to be supported up to now?
            {
                // load data
                await _dataService.LoadNotesAsync(); // load it manually here, because of _dataService.NotesCount

                if (AppSettings.AllowRemoveNotes.Value)
                {
                    var notes = await _dataService.GetAllNotes();

                    if (notes != null)
                    {
                        var diff = _actionCenterService.DiffWithNotesInActionCenter(notes);

                        if (diff.Count > 0)
                        {
                            // unpin deleted notes
                            foreach (var note in diff)
                            {
                                await _tilePinService.UnpinAsync(note.Id);
                            }

                            _dataService.FlagNotesNeedReload();
                            _dataService.FlagArchiveNeedsReload();
                            await _dataService.MoveRangeToArchiveAsync(diff);
                        }

                        if (AppSettings.QuickNotesEnabled.Value &&
                            !_actionCenterService.ContainsQuickNotes())
                        {
                            // add quick notes when it was removed by klicking on it or using it.
                            _actionCenterService.AddQuickNotes();
                        }
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
                        var notes = await _dataService.GetAllNotes();
                        
                        if (notes != null)
                            await _actionCenterService.RefreshAsync(notes);
                    }
                }
            }

            if (hasMutex)
                backgroundMutex.ReleaseMutex();

            deferral.Complete();
        }
    }
}
