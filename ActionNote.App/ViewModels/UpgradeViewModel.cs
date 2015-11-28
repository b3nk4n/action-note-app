using ActionNote.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Store;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class UpgradeViewModel : ViewModelBase
    {
        private ILicenseService _licenseService;

        public ProductItem Product {
            get { return _product; }
            set { Set(ref _product, value); } }
        private ProductItem _product;

        public UpgradeViewModel()
        {
            _licenseService = Injector.Get<ILicenseService>();
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            var items = await _licenseService.LoadProductsAsync(new[] { AppConstants.IAP_PRO_VERSION }, "XXX"); // TODO translate
            // TODO: IAP simulation not working
            
            if (items.Count > 0)
            {
                Product = items[0];
            }
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);
        }
    }
}
