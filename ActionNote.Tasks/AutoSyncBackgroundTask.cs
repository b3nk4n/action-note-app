using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;

namespace ActionNote.Tasks
{
    public sealed class AutoSyncBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ITilePinService _tilePinService;

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
            if (!(DateTimeOffset.Now.Hour > 0 && DateTimeOffset.Now.Hour < 6))
            {
                // sync notes
                var syncResult = await _dataService.SyncNotesAsync();
                if (syncResult.Result == SyncResult.Success || syncResult.Result == SyncResult.Unchanged)
                {
                    if (syncResult.Result == SyncResult.Success)
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

                    if (syncResult.Result != SyncResult.Unchanged || downloadedAFile)
                    {
                        var noteItems = await _dataService.GetAllNotes();
                        await _actionCenterService.Refresh(noteItems);

                        // delete unreferenced tiles
                        var noteIds = await _dataService.GetAllNoteIds();
                        await _tilePinService.UnpinUnreferencedTilesAsync(noteIds);

                        // update changed tiles
                        foreach (var changedNote in syncResult.Data.Changed)
                        {
                            if (_tilePinService.Contains(changedNote.Id))
                                await _tilePinService.UpdateAsync(changedNote);
                        }

                        _tilePinService.UpdateMainTile(noteItems);
                    }
                }
            }

            deferral.Complete();
        }
    }
}
