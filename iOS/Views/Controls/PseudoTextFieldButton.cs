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

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls
{
    public sealed class PseudoTextFieldButton : UIView
    {
        private readonly UILabel _label;

        public string Text
        {
            get => _label?.Text ?? "";
            set
            {
                if (_label == null) { return; }

                if (value != _label.Text)
                {
                    _label.Text = value;
                }
            }
        }

        public PseudoTextFieldButton()
        {
            var backgroundView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.Prominent))
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            AddSubview(backgroundView);

            _label = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = ApplicationTheme.ActionBackgroundColor
            };

            backgroundView.ContentView.AddSubview(_label);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                backgroundView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                backgroundView.TopAnchor.ConstraintEqualTo(TopAnchor),
                backgroundView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                backgroundView.BottomAnchor.ConstraintEqualTo(BottomAnchor),
                _label.LeadingAnchor.ConstraintEqualTo(backgroundView.LeadingAnchor, ApplicationTheme.Margin),
                _label.TopAnchor.ConstraintEqualTo(backgroundView.TopAnchor, ApplicationTheme.Margin),
                _label.BottomAnchor.ConstraintEqualTo(backgroundView.BottomAnchor, -ApplicationTheme.Margin),
                _label.TrailingAnchor.ConstraintEqualTo(backgroundView.TrailingAnchor, -ApplicationTheme.Margin)
            });

            Layer.CornerRadius = ApplicationTheme.CornerRadius;
            Layer.BorderColor = UIColor.SystemGrayColor.CGColor;
            Layer.BorderWidth = 2;
            UserInteractionEnabled = true; //for gesture recognizer
            
            BackgroundColor = UIColor.Clear;
            ClipsToBounds = true;

            var tapRecognizer = new UITapGestureRecognizer(HandleTap);
            AddGestureRecognizer(tapRecognizer);
        }

        private void HandleTap()
        {
            Tapped?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Tapped;
    }
}
