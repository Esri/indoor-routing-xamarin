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

using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls
{
    /// <summary>
    /// Button that is configured to indicate a user action.
    /// This class helps keep the style of these buttons consistent throughout the app.
    /// </summary>
    public sealed class ActionButton : UIButton
    {
        public ActionButton()
        {
            BackgroundColor = ApplicationTheme.ActionBackgroundColor;
            SetTitleColor(ApplicationTheme.ActionForegroundColor, UIControlState.Normal);
            SetTitleColor(ApplicationTheme.ActionForegroundColor.ColorWithAlpha(0.5f), UIControlState.Disabled);
            Layer.CornerRadius = ApplicationTheme.CornerRadius;

            HeightAnchor.ConstraintEqualTo(ApplicationTheme.ActionButtonHeight).Active = true;
        }
    }
}
