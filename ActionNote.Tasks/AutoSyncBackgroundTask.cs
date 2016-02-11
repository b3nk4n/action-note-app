using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Notifications;
using Windows.ApplicationModel.Background;

namespace ActionNote.Tasks
{
    public sealed class AutoSyncBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ITilePinService _tilePinService;
        private IBadgeService _badgeService;

        public AutoSyncBackgroundTask()
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

            // no not auto sync at night to reduce data
            if (!(DateTimeOffset.Now.Hour > 0 && DateTimeOffset.Now.Hour < 6))
            {
                // sync notes
                var syncResult = await _dataService.SyncNotesAsync();
                if (syncResult == SyncResult.Success || syncResult == SyncResult.Unchanged)
                {
                    if (syncResult == SyncResult.Success)
                    {
                        _dataService.FlagNotesNeedReload();
                        _dataService.FlagArchiveNeedsReload();
                    }

                    var downloadedAFile = false;
                    if (await _dataService.UploadMissingAttachements(1) == FileUploadResult.Nop)
                    {
                        // only download anything, when nothing was uploaded (to ensure we do not produce to much traffic / timeout)
                        downloadedAFile = await _dataService.DownloadMissingAttachements();
                    }

                    if (syncResult != SyncResult.Unchanged || downloadedAFile)
                    {
                        var noteItems = await _dataService.GetAllNotes();
                        _actionCenterService.RefreshAsync(noteItems);

                        var noteIds = await _dataService.GetAllNoteIds();
                        await _tilePinService.UnpinUnreferencedTilesAsync(noteIds);

                        var badge = _badgeService.Factory.CreateBadgeNumber(_dataService.NotesCount);
                        _badgeService.GetBadgeUpdaterForApplication().Update(badge);
                    }
                }
            }

            deferral.Complete();
        }
    }
}
