using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;

namespace ActionNote.Tasks
{
    public sealed class TimedRefreshBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;

        public TimedRefreshBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            if (ActionCenterService.LastActionCenterRefresh.Value < DateTimeOffset.Now.AddDays(-2))
            {
                if (AppSettings.ShowNotesInActionCenter.Value ||
                    AppSettings.QuickNotesEnabled.Value)
                {
                    var noteItems = await _dataService.GetAllNotes();
                    _actionCenterService.Refresh(noteItems);
                }
            }

            deferral.Complete();
        }
    }
}
