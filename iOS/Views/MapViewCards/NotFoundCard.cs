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
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    /// <summary>
    /// Card shown when a search finds nothing
    /// </summary>
    public sealed class NotFoundCard : UIView
    {
        private readonly MapViewModel _viewModel;

        private readonly UILabel _headerLabel;

        internal NotFoundCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = ApplicationTheme.HeaderFont,
                TextColor = ApplicationTheme.ForegroundColor,
                Text = "NotFoundTitle".Localize()
            };

            var errorTextView = new UITextView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = ApplicationTheme.ForegroundColor,
                Text = "NotFoundCardMessage".Localize()
            };

            var closeButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            AddSubviews(_headerLabel, errorTextView, closeButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // header label
                _headerLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _headerLabel.CenterYAnchor.ConstraintEqualTo(closeButton.CenterYAnchor),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                // close button
                closeButton.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                closeButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                closeButton.WidthAnchor.ConstraintEqualTo(32),
                closeButton.HeightAnchor.ConstraintEqualTo(32),
                // error description
                errorTextView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                errorTextView.TopAnchor.ConstraintEqualTo(closeButton.BottomAnchor, ApplicationTheme.Margin),
                errorTextView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                // constrains view bottom to bottom of last element
                BottomAnchor.ConstraintEqualTo(errorTextView.BottomAnchor, ApplicationTheme.Margin)
            });

            closeButton.TouchUpInside += Dismiss_Clicked;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Update the UI when the viewmodel state changes
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }

            if (_viewModel.CurrentState == UiState.RouteNotFound)
            {
                _headerLabel.Text = "RouteNotFoundCardTitle".Localize();
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"RouteNotFoundCardTitle".Localize());
            }
            else if (_viewModel.CurrentState == UiState.LocationNotFound)
            {
                _headerLabel.Text = "LocationNotFoundCardTitle".Localize();
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"LocationNotFoundCardTitle".Localize());
            }
        }

        /// <summary>
        /// Forward event to the viewmodel
        /// </summary>
        private void Dismiss_Clicked(object sender, EventArgs e) => _viewModel.DismissNotFound();
    }
}
