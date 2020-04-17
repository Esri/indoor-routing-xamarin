using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls
{
    public class ActionButton : UIButton
    {
        public ActionButton() : base()
        {
            BackgroundColor = ApplicationTheme.ActionBackgroundColor;
            SetTitleColor(ApplicationTheme.ActionForegroundColor, UIControlState.Normal);
            SetTitleColor(ApplicationTheme.ActionForegroundColor.ColorWithAlpha(0.5f), UIControlState.Disabled);
            Layer.CornerRadius = ApplicationTheme.CornerRadius;

            HeightAnchor.ConstraintEqualTo(ApplicationTheme.ActionButtonHeight).Active = true;
        }
    }
}
