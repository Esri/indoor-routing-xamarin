using System;
using Foundation;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public static class LocalizationExtension
    {
        /// <summary>
        /// Returns the localized string for the given key. If no localized string is found, the key is returned.
        /// </summary>
        public static string AsLocalized(this string localizationKey)
        {
            return NSBundle.MainBundle.GetLocalizedString(localizationKey);
        }
    }
}
