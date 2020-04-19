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

            var dismissButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            AddSubviews(_headerLabel, errorTextView, dismissButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _headerLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _headerLabel.CenterYAnchor.ConstraintEqualTo(dismissButton.CenterYAnchor),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(dismissButton.LeadingAnchor, -ApplicationTheme.Margin),
                dismissButton.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                dismissButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                dismissButton.WidthAnchor.ConstraintEqualTo(32),
                dismissButton.HeightAnchor.ConstraintEqualTo(32),
                errorTextView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                errorTextView.TopAnchor.ConstraintEqualTo(dismissButton.BottomAnchor, ApplicationTheme.Margin),
                errorTextView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                BottomAnchor.ConstraintEqualTo(errorTextView.BottomAnchor, ApplicationTheme.Margin)
            });

            dismissButton.TouchUpInside += Dismiss_Clicked;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }

            if (_viewModel.CurrentState == UiState.RouteNotFound)
            {
                _headerLabel.Text = "RouteNotFoundCardTitle".Localize();
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"Route couldn't be found".Localize());
            }
            else if (_viewModel.CurrentState == UiState.LocationNotFound)
            {
                _headerLabel.Text = "LocationNotFoundCardTitle".Localize();
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"Location couldn't be found".Localize());
            }
        }

        private void Dismiss_Clicked(object sender, EventArgs e) => _viewModel.DismissNotFound();
    }
}
