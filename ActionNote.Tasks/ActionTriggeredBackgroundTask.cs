using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using UWPCore.Framework.Common;
using UWPCore.Framework.IoC;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionTriggeredBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;

        private Localizer _localizer = new Localizer("ActionNote.Common");

        public ActionTriggeredBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<IDataService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details != null &&
                AppSettings.ShowNotesInActionCenter.Value)
            {
                if (details.Argument == "quickNote")
                {
                    if (details.UserInput.ContainsKey("content"))
                    {
                        string content = null;
                        string title = null;
                        var input = details.UserInput["content"] as string;


                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            if (AppSettings.QuickNotesContentType.Value == AppConstants.QUICK_NOTES_TITLE_AND_CONTENT)
                            {
                                var splitIndex = input.IndexOf("\r\n");

                                if (splitIndex != -1)
                                {
                                    title = input.Substring(0, splitIndex);
                                    content = input.Substring(splitIndex + 2, input.Length - splitIndex - 2);
                                }

                                if (string.IsNullOrWhiteSpace(content))
                                {
                                    title = _localizer.Get("QuickNote");
                                    content = title;
                                }
                                else if (string.IsNullOrWhiteSpace(title))
                                {
                                    title = _localizer.Get("QuickNote");
                                    content = title;
                                }
                            }
                            else
                            {
                                title = _localizer.Get("QuickNote");
                                content = input;
                            }

                            if (!string.IsNullOrWhiteSpace(title) ||
                                !string.IsNullOrWhiteSpace(content))
                            {
                                var noteItem = new NoteItem(title, content);
                                _dataService.FlagNotesHaveChangedInBackground();
                                await _dataService.AddNoteAsync(noteItem);

                                if (AppSettings.SortNoteInActionCenterBy.Value == AppConstants.SORT_DATE)
                                {
                                    // add it into the action center at the beginning when we order for date.
                                    await _actionCenterService.AddToTop(noteItem);
                                }
                                else
                                {
                                    // refresh all, because new note could be not at the top of the list
                                    var notes = await _dataService.GetAllNotes();
                                    if (notes != null)
                                        await _actionCenterService.RefreshAsync(notes);
                                }
                            }
                        }
                    }
                }
            }

            deferral.Complete();
        }
    }
}
