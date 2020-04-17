using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    
    public static class ApplicationTheme
    {
        public static nint Margin = 8;
        public static UIColor BackgroundColor = UIColor.SystemBackgroundColor;
        public static UIColor ForegroundColor = UIColor.LabelColor;
        public static UIColor SeparatorColor = UIColor.SystemGray2Color;
        public static UIBlurEffect PanelBackgroundMaterial = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial);
        public static nint SideWidgetWidth = 48;
        public static nint FloorWidthMaxHeight = 240;
        public static double SimpleAnimationDuration = 0.5;
        public static nint HandlebarThickness = 4;
        public static nint HandlebarLength = 48;
        public static nint HandlebarCornerRadius = 2;
        public static nint CornerRadius = 8;
        public static UIColor ActionBackgroundColor = new UIColor(0.071f, 0.475f, 0.757f, 1.00f);
        public static UIColor ActionForegroundColor = UIColor.White;
        public static UIColor SelectionBackgroundColor = ActionBackgroundColor;
        public static UIColor SelectionForegroundColor = ActionForegroundColor;
        public static nint ActionButtonHeight = 44;
        public static UIFont HeaderFont = UIFont.BoldSystemFontOfSize(28);
    }
    /*
    public static class ApplicationTheme
    {
        public static nint Margin = 16;
        public static UIColor BackgroundColor = UIColor.SystemOrangeColor;
        public static UIColor ForegroundColor = UIColor.SystemPinkColor;
        public static UIColor SeparatorColor = UIColor.SystemGray2Color;
        public static UIBlurEffect PanelBackgroundMaterial = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial);
        public static nint SideWidgetWidth = 56;
        public static nint FloorWidthMaxHeight = 120;
        public static double SimpleAnimationDuration = 2.5;
        public static nint HandlebarThickness = 6;
        public static nint HandlebarLength = 22;
        public static nint HandlebarCornerRadius = 2;
        public static nint CornerRadius = 1;
        public static UIColor ActionBackgroundColor = UIColor.SystemRedColor;
        public static UIColor ActionForegroundColor = UIColor.SystemBlueColor;
        public static UIColor SelectionBackgroundColor = ActionBackgroundColor;
        public static UIColor SelectionForegroundColor = ActionForegroundColor;
        public static nint ActionButtonHeight = 22;
        public static UIFont HeaderFont = UIFont.ItalicSystemFontOfSize(32);
    }
    */
}
