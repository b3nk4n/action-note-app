using UWPCore.Framework.Store;

namespace ActionNote.Common.Services.Store
{
    /// <summary>
    /// Simple marker interface for Ninject
    /// </summary>
    public interface ICachedLicenseService : ILicenseService
    {
        /// <summary>
        /// Invalidate the cached IAP value and force to reload it the next time.
        /// </summary>
        void Invalidate();
    }
}
