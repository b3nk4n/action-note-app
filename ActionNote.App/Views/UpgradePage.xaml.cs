using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UpgradePage : UniversalPage
    {
        public UpgradePage()
        {
            InitializeComponent();
            DataContext = new UpgradeViewModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HeartBeat.Begin();
        }
    }
}
