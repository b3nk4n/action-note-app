using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private INoteDataService _dataService;
        private ITilePinService _tilePinService;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<INoteDataService>();
            _tilePinService = injector.Get<ITilePinService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            // wait to ensure ActionTriggeredBackgroundTask is running first, that restores items that have
            // been deleted by clicking on them.
            await Task.Delay(1000);

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;

            if (details != null &&
                !_actionCenterService.IsRemoveBlocked() &&
                AppSettings.SyncWithActionCenter.Value &&
                details.ChangeType == ToastHistoryChangedType.Removed) // Remark: ToastHistoryChangedType.Cleared seems not to be supported up to now?
            {
                // load data
                await _dataService.Notes.Load();

                if (AppSettings.AllowRemoveNotes.Value)
                {
                    var diff = _actionCenterService.DiffWithNotesInActionCenter(_dataService.Notes.GetAll());

                    foreach (var noteId in diff)
                    {
                        await _tilePinService.UnpinAsync(noteId);
                        _dataService.MoveToArchiv(_dataService.Notes.Get(noteId));
                        _dataService.FlagNotesHaveChangedInBackground();
                        _dataService.FlagArchiveHasChangedInBackground();
                    }

                    if (AppSettings.QuickNotesEnabled.Value &&
                        !_actionCenterService.ContainsQuickNotes())
                    {
                        // add quick notes when it was removed by klicking on it or using it.
                        _actionCenterService.AddQuickNotes();
                    }
                }
                else
                {
                    // only refresh when there has been a change (because the bgtask is called multiple times when we do multiple remove operations)
                    if (_actionCenterService.NotesCount == _dataService.Notes.Count)
                    {
                        // when only the quick notes was removed, just re-add it
                        if (AppSettings.QuickNotesEnabled.Value && !_actionCenterService.ContainsQuickNotes())
                        {
                            _actionCenterService.AddQuickNotes();
                        }
                    }
                    else
                    {
                        await _actionCenterService.RefreshAsync(_dataService.Notes.GetAll());
                    }
                }
            }

            deferral.Complete();
        }
    }
}
