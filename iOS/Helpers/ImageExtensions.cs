using System;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Convert images to byte array to be used as map pins.
        /// </summary>
        /// <returns>The to byte array.</returns>
        /// <param name="image">Input Image.</param>
        public static byte[] ToByteArray(this UIImage image)
        {
            using (NSData imageData = image.AsPNG())
            {
                return imageData.ToArray();
            }
        }
    }
}
