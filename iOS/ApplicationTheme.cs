// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    /// <summary>
    /// Defines common styling parameters; modify this class to change colors, materials, spacing, and corner rounding
    /// </summary>
    public static class ApplicationTheme
    {
        public static nint Margin;
        public static UIColor BackgroundColor;
        public static UIColor ForegroundColor;
        public static UIColor SeparatorColor;
        public static UIBlurEffect PanelBackgroundMaterial;
        public static nint SideWidgetWidth;
        public static nint FloorWidthMaxHeight;
        public static nint HandlebarThickness;
        public static nint HandlebarLength;
        public static nint HandlebarCornerRadius;
        public static nint CornerRadius;
        public static UIColor ActionBackgroundColor;
        public static UIColor ActionForegroundColor;
        public static UIColor SelectionBackgroundColor;
        public static UIColor SelectionForegroundColor;
        public static UIColor PrimaryLabelColor;
        public static UIColor SecondaryLabelColor;
        // Accessory button is a light/dark responsive color defined in the asset catalog
        public static UIColor AccessoryButtonColor;
        public static nint ActionButtonHeight;
        public static UIFont HeaderFont;

        static ApplicationTheme()
        {
            Margin = 8;
            SideWidgetWidth = 48;
            FloorWidthMaxHeight = 240;
            HandlebarThickness = 4;
            HandlebarLength = 48;
            HandlebarCornerRadius = 2;
            CornerRadius = 8;
            

            // Accessory button is a light/dark responsive color defined in the asset catalog
            AccessoryButtonColor = UIColor.FromName("AccessoryButtonColor");
            ActionBackgroundColor = AccessoryButtonColor;
            ActionForegroundColor = UIColor.White;
            SelectionBackgroundColor = ActionBackgroundColor;
            SelectionForegroundColor = ActionForegroundColor;

            ActionButtonHeight = 44;
            HeaderFont = UIFont.PreferredTitle1;

            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                BackgroundColor = UIColor.SystemBackgroundColor;
                ForegroundColor = UIColor.LabelColor;
                SeparatorColor = UIColor.SystemGray2Color;
                PanelBackgroundMaterial = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial);
                PrimaryLabelColor = UIColor.LabelColor;
                SecondaryLabelColor = UIColor.SecondaryLabelColor;
            }
            else
            {
                BackgroundColor = UIColor.White;
                ForegroundColor = UIColor.Black;
                SeparatorColor = UIColor.LightGray;
                PanelBackgroundMaterial = UIBlurEffect.FromStyle(UIBlurEffectStyle.Prominent);
                PrimaryLabelColor = UIColor.Black;
                SecondaryLabelColor = UIColor.DarkGray;
            }
            
        }
    }
}
