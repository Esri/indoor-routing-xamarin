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

using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls
{
    /// <summary>
    /// UIButton pre-configured as a close button (circle with x in the center)
    /// </summary>
    public sealed class CloseButton : UIButton
    {
        private const int CircleButtonRadius = 16;

        public CloseButton()
        {
            BackgroundColor = UIColor.SystemGray4Color;
            Layer.CornerRadius = CircleButtonRadius;
            SetImage(UIImage.FromBundle("x"), UIControlState.Normal);
            TintColor = UIColor.SystemGrayColor;
        }

        public override CGSize IntrinsicContentSize => new CGSize(2 * CircleButtonRadius, 2 * CircleButtonRadius);
    }
}
