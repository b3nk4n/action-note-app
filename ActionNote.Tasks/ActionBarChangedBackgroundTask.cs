using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using UWPCore.Framework.Storage;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionBarChangedBackgroundTask : IBackgroundTask
    {
        private IToastUpdateService _toastUpdateService;

        public ActionBarChangedBackgroundTask()
        {
            IInjector injector = new Injector(new DefaultModule(), new AppModule());
            _toastUpdateService = injector.Get<IToastUpdateService>();
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;
            if (details != null)
            {
                if (details.ChangeType == ToastHistoryChangedType.Cleared) // TODO: check, why the change type is never clear!?
                {
                    if (!AppSettings.AllowClearNotes.Value)
                    {
                        Logger.WriteLine("Clear - refresh");
                        _toastUpdateService.Refresh();
                    }
                    else
                    {
                        Logger.WriteLine("Clear - delete missing refresh");
                        _toastUpdateService.DeleteNotesThatAreMissingInActionCenter();
                        _toastUpdateService.Refresh();
                    }
                }
                else if (details.ChangeType == ToastHistoryChangedType.Removed)
                {
                    if (!AppSettings.AllowRemoveNotes.Value)
                    {
                        Logger.WriteLine("Remove - refresh");
                        _toastUpdateService.Refresh();
                    }
                    else
                    {
                        Logger.WriteLine("Remove - delete missing refresh");
                        _toastUpdateService.DeleteNotesThatAreMissingInActionCenter();
                        _toastUpdateService.Refresh();
                    }
                }
            }

            deferral.Complete();
        }
    }
}
