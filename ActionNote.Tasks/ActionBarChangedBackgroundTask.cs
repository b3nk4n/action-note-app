using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private INoteDataService _dataService;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<INoteDataService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            // wait to ensure ActionTriggeredBackgroundTask is running first, that restores items that have
            // been deleted by clicking on them.
            await Task.Delay(1000); // TODO: still needed?

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;

            if (details != null &&
                !_actionCenterService.IsRemoveBlocked() &&
                details.ChangeType == ToastHistoryChangedType.Removed) // Remark: ToastHistoryChangedType.Cleared seems not to be supported up to now?
            {
                // load data
                await _dataService.Notes.Load();

                if (AppSettings.AllowRemoveNotes.Value)
                {
                    Logger.WriteLine("Remove - delete missing refresh");
                    _actionCenterService.DeleteNotesThatAreMissingInActionCenter(_dataService.Notes);

                    if (AppSettings.QuickNotesEnabled.Value &&
                        !_actionCenterService.ContainsQuickNotes())
                    {
                        // add quick notes when it was removed by klicking on it or using it.
                        _actionCenterService.AddQuickNotes();
                    }
                }
                else
                {
                    Logger.WriteLine("Remove - refresh");
                    await _actionCenterService.RefreshAsync(_dataService.Notes); // TODO: not sure here?
                }
            }
            else
            {
                Logger.WriteLine("BG TASK BLOCKED!");
            }

            deferral.Complete();
        }
    }
}
