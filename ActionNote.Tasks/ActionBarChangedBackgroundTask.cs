using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.IoC;
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
                if (details.ChangeType != ToastHistoryChangedType.Added)
                {
                    _toastUpdateService.Refresh();
                }

            }

            deferral.Complete();
        }
    }
}
