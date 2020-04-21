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
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    public sealed class LocationInfoCard : UIView
    {
        private readonly MapViewModel _viewModel;
        private readonly ActionButton _startDirectionsButton;
        private readonly UILabel _primaryLabel;
        private readonly UILabel _secondaryLabel;

        internal LocationInfoCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _startDirectionsButton = new ActionButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _startDirectionsButton.SetTitle("FindDirectionsButtonText".Localize(), UIControlState.Normal);

            var closeButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _primaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor,
                Font = ApplicationTheme.HeaderFont
            };

            _secondaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            AddSubviews(_startDirectionsButton, closeButton, _primaryLabel, _secondaryLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _primaryLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _primaryLabel.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                _primaryLabel.TrailingAnchor.ConstraintEqualTo(closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                //
                _secondaryLabel.LeadingAnchor.ConstraintEqualTo(_primaryLabel.LeadingAnchor),
                _secondaryLabel.TrailingAnchor.ConstraintEqualTo(_primaryLabel.TrailingAnchor),
                _secondaryLabel.TopAnchor.ConstraintEqualTo(_primaryLabel.BottomAnchor, ApplicationTheme.Margin),
                //
                closeButton.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                closeButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                closeButton.WidthAnchor.ConstraintEqualTo(32),
                closeButton.HeightAnchor.ConstraintEqualTo(32),
                //
                _startDirectionsButton.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_secondaryLabel.BottomAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(closeButton.BottomAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.HeightAnchor.ConstraintEqualTo(44),
                //
                BottomAnchor.ConstraintEqualTo(_startDirectionsButton.BottomAnchor, ApplicationTheme.Margin)
            });

            // Handle closing location card.
            closeButton.TouchUpInside += Close_Clicked;

            // Check settings
            _startDirectionsButton.Enabled = AppSettings.CurrentSettings.IsRoutingEnabled;

            // Wait for app state changes (particularly identify feature results)
            _viewModel.PropertyChanged += ViewModel_Changed;

            AppSettings.CurrentSettings.PropertyChanged += CurrentSettings_PropertyChanged;

            // Handle searching for directions
            _startDirectionsButton.TouchUpInside += SearchDirections_Clicked;
        }

        private void CurrentSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.IsRoutingEnabled))
            {
                _startDirectionsButton.Enabled = AppSettings.CurrentSettings.IsRoutingEnabled;
            }
        }

        private void ViewModel_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentRoom))
            {
                return;
            }

            _primaryLabel.Text = _viewModel.CurrentRoom?.PrimaryDisplayField ?? string.Empty;
            _secondaryLabel.Text = _viewModel.CurrentRoom?.SecondaryDisplayField ?? string.Empty;

            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"Room found".Localize());
        }

        private void SearchDirections_Clicked(object sender, EventArgs e) => _viewModel.StartSearchFromFoundFeature();

        private void Close_Clicked(object sender, EventArgs e) => _viewModel.CloseLocationInfo();
    }
}
