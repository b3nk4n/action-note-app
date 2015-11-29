using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using UWPCore.Framework.Common;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class AutoSyncBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ITilePinService _tilePinService;

        private Localizer _localizer = new Localizer("ActionNote.Common");

        public AutoSyncBackgroundTask()
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

            // no not auto sync at night to reduce data
            if (DateTimeOffset.Now.Hour > 0 && DateTimeOffset.Now.Hour < 6)
            {
                deferral.Complete();
                return;
            }

            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details != null)
            {
                // sync notes
                var syncResult = await _dataService.SyncNotesAsync();
                if (syncResult == SyncResult.Success || syncResult == SyncResult.Unchanged)
                {
                    if (syncResult == SyncResult.Success)
                    {
                        _dataService.FlagNotesHaveChangedInBackground();
                        _dataService.FlagArchiveHasChangedInBackground();
                    }

                    await _dataService.UploadMissingAttachements();
                    var downloadedAFile = await _dataService.DownloadMissingAttachements();

                    if (syncResult != SyncResult.Unchanged || downloadedAFile)
                    {
                        var noteItems = await _dataService.GetAllNotes();
                        await _actionCenterService.RefreshAsync(noteItems);

                        var noteIds = await _dataService.GetAllNoteIds();
                        await _tilePinService.UnpinUnreferencedTilesAsync(noteIds);
                    }
                }
            }

            deferral.Complete();
        }
    }
}
