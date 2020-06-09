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

using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    /// <summary>
    /// View for showing information about a particular room
    /// </summary>
    public sealed class LocationInfoCard : UIView
    {
        private readonly MapViewModel _viewModel;

        private readonly ActionButton _startDirectionsButton;
        private readonly UILabel _primaryLabel;
        private readonly UILabel _secondaryLabel;

        public LocationInfoCard(MapViewModel viewModel)
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
                // primary label
                _primaryLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _primaryLabel.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                _primaryLabel.TrailingAnchor.ConstraintEqualTo(closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                _primaryLabel.HeightAnchor.ConstraintEqualTo(40),
                // secondary label
                _secondaryLabel.LeadingAnchor.ConstraintEqualTo(_primaryLabel.LeadingAnchor),
                _secondaryLabel.TrailingAnchor.ConstraintEqualTo(_primaryLabel.TrailingAnchor),
                _secondaryLabel.TopAnchor.ConstraintEqualTo(_primaryLabel.BottomAnchor, ApplicationTheme.Margin),
                // close button
                closeButton.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                closeButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                closeButton.WidthAnchor.ConstraintEqualTo(32),
                closeButton.HeightAnchor.ConstraintEqualTo(32),
                // directions button
                _startDirectionsButton.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                _startDirectionsButton.TopAnchor.ConstraintEqualTo(_secondaryLabel.BottomAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.HeightAnchor.ConstraintEqualTo(44),
                // constrains view bottom to bottom of last element
                BottomAnchor.ConstraintEqualTo(_startDirectionsButton.BottomAnchor, ApplicationTheme.Margin)
            });

            // Handle closing location card.
            closeButton.TouchUpInside += Close_Clicked;

            // Check settings
            _startDirectionsButton.Hidden = !AppSettings.CurrentSettings.IsRoutingEnabled;

            // Wait for app state changes (particularly identify feature results)
            _viewModel.PropertyChanged += ViewModel_Changed;

            AppSettings.CurrentSettings.PropertyChanged += CurrentSettings_PropertyChanged;

            // Handle searching for directions
            _startDirectionsButton.TouchUpInside += SearchDirections_Clicked;
        }

        /// <summary>
        /// Updates UI for settings changes
        /// </summary>
        private void CurrentSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.IsRoutingEnabled))
            {
                _startDirectionsButton.Hidden = !AppSettings.CurrentSettings.IsRoutingEnabled;
            }

            RelayoutRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates UI for viewmodel property changes
        /// </summary>
        private void ViewModel_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentRoom))
            {
                return;
            }

            _primaryLabel.Text = _viewModel.CurrentRoom?.PrimaryDisplayField;
            _secondaryLabel.Text = _viewModel.CurrentRoom?.SecondaryDisplayField;

            // Since values have changed, UI may now need more space.
            RelayoutRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Forwards direction search start request to viewmodel
        /// </summary>
        private void SearchDirections_Clicked(object sender, EventArgs e) => _viewModel.StartSearchFromFoundFeature();

        /// <summary>
        /// Forwards location close event to viewmodel
        /// </summary>
        private void Close_Clicked(object sender, EventArgs e) => _viewModel.CloseLocationInfo();

        /// <summary>
        /// Raised when content has changed and the containing view needs to remeasure
        /// </summary>
        public event EventHandler RelayoutRequested;
    }
}
