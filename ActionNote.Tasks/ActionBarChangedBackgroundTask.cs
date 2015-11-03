using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _toastUpdateService = injector.Get<IToastUpdateService>();
            _notesRepository = injector.Get<INotesRepository>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            // wait to ensure ActionTriggeredBackgroundTask is running first, that restores items that have
            // been deleted by clicking on them.
            await Task.Delay(2000);

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;
            if (details != null)
            {
                if (details.ChangeType == ToastHistoryChangedType.Cleared ||
                    details.ChangeType == ToastHistoryChangedType.Removed)
                {
                    // load data
                    await _notesRepository.Load();
                }

                if (details.ChangeType == ToastHistoryChangedType.Cleared) // TODO: check, why the change type is never clear!?
                {
                    if (!AppSettings.AllowClearNotes.Value)
                    {
                        Logger.WriteLine("Clear - refresh");
                        _toastUpdateService.Refresh(_notesRepository);
                    }
                    else
                    {
                        Logger.WriteLine("Clear - delete missing refresh");
                        _toastUpdateService.DeleteNotesThatAreMissingInActionCenter(_notesRepository);
                        _toastUpdateService.Refresh(_notesRepository);
                    }
                }
                else if (details.ChangeType == ToastHistoryChangedType.Removed)
                {
                    if (!AppSettings.AllowRemoveNotes.Value)
                    {
                        Logger.WriteLine("Remove - refresh");
                        _toastUpdateService.Refresh(_notesRepository);
                    }
                    else
                    {
                        Logger.WriteLine("Remove - delete missing refresh");
                        _toastUpdateService.DeleteNotesThatAreMissingInActionCenter(_notesRepository);
                        _toastUpdateService.Refresh(_notesRepository);
                    }
                }

                if (details.ChangeType == ToastHistoryChangedType.Cleared ||
                    details.ChangeType == ToastHistoryChangedType.Removed)
                {
                    // load data
                    await _notesRepository.Save();
                }
            }

            deferral.Complete();
        }
    }
}
