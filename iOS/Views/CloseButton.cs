using System;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    /// <summary>
    /// UIButton pre-configured as a close button
    /// </summary>
    public class CloseButton : UIButton
    {
        public CloseButton() : base()
        {
            BackgroundColor = UIColor.SystemGray4Color;
            Layer.CornerRadius = 16;
            SetImage(UIImage.FromBundle("x"), UIControlState.Normal);
            TintColor = UIColor.SystemGrayColor;
        }

        public override CGSize IntrinsicContentSize => new CGSize(32, 32);
    }
}
