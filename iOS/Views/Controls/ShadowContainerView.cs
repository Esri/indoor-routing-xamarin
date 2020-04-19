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
    /// Custom view container applies a standard shadow to the contained view
    /// </summary>
    public sealed class ShadowContainerView : UIView
    {
        public ShadowContainerView(UIView childView)
        {
            TranslatesAutoresizingMaskIntoConstraints = false;

            Layer.ShadowColor = UIColor.Black.CGColor;
            Layer.ShadowRadius = 1;
            Layer.ShadowOpacity = 0.5f;
            Layer.ShadowOffset = new CoreGraphics.CGSize(0, 0);

            var innerContainer = new UIView {ClipsToBounds = true};
            innerContainer.Layer.CornerRadius = ApplicationTheme.CornerRadius;
            innerContainer.TranslatesAutoresizingMaskIntoConstraints = false;

            AddSubview(innerContainer);

            innerContainer.AddSubview(childView);
            childView.TranslatesAutoresizingMaskIntoConstraints = false;

            childView.Layer.CornerRadius = ApplicationTheme.CornerRadius;
            childView.ClipsToBounds = true;

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                childView.LeadingAnchor.ConstraintEqualTo(innerContainer.LeadingAnchor),
                childView.TrailingAnchor.ConstraintEqualTo(innerContainer.TrailingAnchor),
                childView.TopAnchor.ConstraintEqualTo(innerContainer.TopAnchor),
                childView.BottomAnchor.ConstraintEqualTo(innerContainer.BottomAnchor),
                innerContainer.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                innerContainer.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                innerContainer.BottomAnchor.ConstraintEqualTo(BottomAnchor),
                innerContainer.TopAnchor.ConstraintEqualTo(TopAnchor)
            });
        }
    }
}
