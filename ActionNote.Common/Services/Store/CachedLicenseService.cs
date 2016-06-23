using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Storage;
using UWPCore.Framework.Store;

namespace ActionNote.Common.Services.Store
{
    /// <summary>
    /// A lincese service that caches the Pro-Version active status.
    /// This is due to a bug in Windows 10 Mobile, which takes very long the first time,
    /// and in this case leading to a very low startup performance.
    /// </summary>
    /// <remarks>
    /// This inherited class might be not neccessary after Microsoft fixes this issue.
    /// <see cref="https://social.msdn.microsoft.com/Forums/en-US/dc6c1177-ff5a-4fce-bf8b-8a265cbfd6e4/uwpw10m-first-access-to-currentapplicenseinformation-very-slow-on-windows-10-mobile?forum=wpdevelop"/>
    /// </remarks>
    public class CachedLicenseService : LicenseService, ICachedLicenseService
    {
        private static StoredObjectBase<bool?> IsProVersionActive = new LocalObject<bool?>("_p_cache_", null);

        [Inject]
        public CachedLicenseService(ILocalStorageService localStorageService)
            : base(localStorageService)
        {
        }

        public void Invalidate()
        {
            IsProVersionActive.Value = null;
        }

        public override bool IsProductActive(string productId)
        {
            var isProVersion = IsProVersionActive.Value;

            if (isProVersion.HasValue)
                return isProVersion.Value;

            var result = base.IsProductActive(productId);
            IsProVersionActive.Value = result;

            return result;
        }

        public async override Task<bool> RequestProductPurchaseAsync(string productId)
        {
            var status = await base.RequestProductPurchaseAsync(productId);
            Invalidate();
            return status;
        }
    }
}
