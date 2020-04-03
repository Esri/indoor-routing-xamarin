using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public static class ViewExtensions
    {
        public static UIView EncapsulateInShadowView(this UIView viewToEncapsulate)
        {
            return new ShadowContainerView(viewToEncapsulate);
        }
    }
}
