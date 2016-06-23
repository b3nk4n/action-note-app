using ActionNote.Common;
using ActionNote.Common.Services;
using ActionNote.Common.Services.Store;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Launcher;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using UWPCore.Framework.Store;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class UpgradeViewModel : ViewModelBase
    {
        private ICachedLicenseService _licenseService;
        private IDataService _dataService;

        private Localizer _localizer = new Localizer();

        public ProductItem Product {
            get { return _product; }
            set { Set(ref _product, value); } }
        private ProductItem _product;

        public UpgradeViewModel()
        {
            _licenseService = Injector.Get<ICachedLicenseService>();
            _dataService = Injector.Get<IDataService>();

            ReadMoreCommand = new DelegateCommand(async () =>
            {
                await SystemLauncher.LaunchUriAsync(new Uri("http://bsautermeister.de/actionnote/", UriKind.Absolute));
            });

            UpgradeCommand = new DelegateCommand(async () =>
            {
                if (await _licenseService.RequestProductPurchaseAsync(AppConstants.IAP_PRO_VERSION))
                {
                    ShowHeartBeat = true;

                    // enable online sync directly after purchase
                    AppSettings.SyncEnabled.Value = true;
                }
            });
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            if (!_dataService.IsProVersion)
            {
                var items = await _licenseService.LoadProductsAsync(new[] { AppConstants.IAP_PRO_VERSION }, _localizer.Get("IAP.Purchased"));

                if (items != null &&
                    items.Count > 0)
                {
                    Product = items[0];
                }
                // when there is an error (like in debug mode), use static data
                else if (items == null)
                {
                    Product = new ProductItem()
                    {
                        Name = "Pro Version",
                        Description = "Maximize your productivity and work on all your Windows 10 devices at the same time. Synchronize your notes automatically with the Action Center of your PC, tablet and smartphone.",
                        ImageUri = new Uri(IOConstants.APPX_SCHEME +  "/Assets/Images/StoreLogo300.png", UriKind.Absolute),
                        Id = AppConstants.IAP_PRO_VERSION,
                        IsActive = false,
                        Status = "2.99$"
                    };
                }
            }
            else
            {
                // Show thank you!
                ShowHeartBeat = true;
            }

            // force to reload the Pro-Version status
            // (also here to make sure that this method is called by 100%)
            _licenseService.Invalidate();
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);

            // force to reload the Pro-Version status
            _licenseService.Invalidate();
        }

        public bool ShowHeartBeat
        {
            get { return _showHeartBeat; }
            set { Set(ref _showHeartBeat, value); }
        }
        private bool _showHeartBeat;

        public ICommand UpgradeCommand { get; private set; }

        public ICommand ReadMoreCommand { get; private set; }
    }
}
