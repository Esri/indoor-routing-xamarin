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
    /// <summary>
    /// Card for planning a route, which means choosing an origin and a destination
    /// </summary>
    public sealed class RoutePlanningCard : UIView
    {
        private readonly MapViewModel _viewModel;

        // Buttons that look like text fields; when the user touches them, the location search card is shown
        private readonly PseudoTextFieldButton _startTextPlaceholder;
        private readonly PseudoTextFieldButton _endTextPlaceholder;

        internal RoutePlanningCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _startTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _endTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };

            var searchRouteButton = new ActionButton { TranslatesAutoresizingMaskIntoConstraints = false };
            searchRouteButton.SetTitle("SearchForRouteButtonText".Localize(), UIControlState.Normal);

            UIButton cancelRouteSearchButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            var searchStartLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "OriginRouteSearchFieldLabel".Localize() };
            var searchEndLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "DestinationRouteSearchFieldLabel".Localize() };

            searchStartLabel.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);

            var routeSearchHeader = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Text = "DirectionsPanelHeader".Localize(),
                TextColor = ApplicationTheme.ForegroundColor,
                Font = ApplicationTheme.HeaderFont
            };

            // swap origin and destination button
            var swapOriginDestinationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            swapOriginDestinationButton.SetImage(UIImage.FromBundle("arrow-up-down"), UIControlState.Normal);
            swapOriginDestinationButton.TintColor = ApplicationTheme.ForegroundColor;

            AddSubviews(_startTextPlaceholder, _endTextPlaceholder, searchRouteButton, searchStartLabel, searchEndLabel, cancelRouteSearchButton, routeSearchHeader, swapOriginDestinationButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // label
                routeSearchHeader.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                routeSearchHeader.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                routeSearchHeader.TrailingAnchor.ConstraintEqualTo(cancelRouteSearchButton.LeadingAnchor, -ApplicationTheme.Margin),
                // close button
                cancelRouteSearchButton.CenterYAnchor.ConstraintEqualTo(routeSearchHeader.CenterYAnchor),
                cancelRouteSearchButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                cancelRouteSearchButton.HeightAnchor.ConstraintEqualTo(32),
                cancelRouteSearchButton.WidthAnchor.ConstraintEqualTo(32),
                // labels
                searchStartLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                searchStartLabel.CenterYAnchor.ConstraintEqualTo(_startTextPlaceholder.CenterYAnchor),
                searchEndLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                searchEndLabel.CenterYAnchor.ConstraintEqualTo(_endTextPlaceholder.CenterYAnchor),
                searchEndLabel.TrailingAnchor.ConstraintEqualTo(searchStartLabel.TrailingAnchor),
                // search bars
                _startTextPlaceholder.LeadingAnchor.ConstraintEqualTo(searchStartLabel.TrailingAnchor, ApplicationTheme.Margin),
                _startTextPlaceholder.TopAnchor.ConstraintEqualTo(routeSearchHeader.BottomAnchor, ApplicationTheme.Margin),
                _startTextPlaceholder.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                _startTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                _endTextPlaceholder.LeadingAnchor.ConstraintEqualTo(_startTextPlaceholder.LeadingAnchor),
                _endTextPlaceholder.TrailingAnchor.ConstraintEqualTo(_startTextPlaceholder.TrailingAnchor),
                _endTextPlaceholder.TopAnchor.ConstraintEqualTo(_startTextPlaceholder.BottomAnchor, ApplicationTheme.Margin),
                _endTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                // search button
                searchRouteButton.TrailingAnchor.ConstraintEqualTo(swapOriginDestinationButton.LeadingAnchor),
                searchRouteButton.TopAnchor.ConstraintEqualTo(_endTextPlaceholder.BottomAnchor, ApplicationTheme.Margin),
                searchRouteButton.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                // swap origin and destinations button
                swapOriginDestinationButton.HeightAnchor.ConstraintEqualTo(44),
                swapOriginDestinationButton.WidthAnchor.ConstraintEqualTo(44),
                swapOriginDestinationButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                swapOriginDestinationButton.CenterYAnchor.ConstraintEqualTo(searchRouteButton.CenterYAnchor),
                // constrains view bottom to bottom of last element
                BottomAnchor.ConstraintEqualTo(searchRouteButton.BottomAnchor, ApplicationTheme.Margin)
            });

            searchRouteButton.TouchUpInside += SearchRouteButton_Clicked;
            cancelRouteSearchButton.TouchUpInside += CancelRouteSearchButton_Clicked;
            swapOriginDestinationButton.TouchUpInside += SwapOriginDestinationButton_Clicked;
            _startTextPlaceholder.Tapped += EditOriginField_Clicked;
            _endTextPlaceholder.Tapped += EditDestinationField_Clicked;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Update the UI when viewmodel properties change
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.OriginSearchText) || e.PropertyName == nameof(_viewModel.DestinationSearchText))
            {
                _startTextPlaceholder.Text = _viewModel.OriginSearchText;
                _endTextPlaceholder.Text = _viewModel.DestinationSearchText;
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"RoutePlanningStartedAccessibilityAnnouncement".Localize());
            }
        }

        /// <summary>
        /// Forwards event to viewmodel
        /// </summary>
        private void EditOriginField_Clicked(object sender, EventArgs e) => _viewModel.SelectOriginSearch();

        /// <summary>
        /// Forwards event to viewmodel
        /// </summary>
        private void EditDestinationField_Clicked(object sender, EventArgs e) => _viewModel.SelectDestinationSearch();

        /// <summary>
        /// Forwards command to viewmodel
        /// </summary>
        private async void SearchRouteButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.PerformRouteSearch();
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Forwards command to viewmodel
        /// </summary>
        private void CancelRouteSearchButton_Clicked(object sender, EventArgs e) => _viewModel.CancelRouteSearch();

        /// <summary>
        /// Forwards command to viewmodel
        /// </summary>
        private void SwapOriginDestinationButton_Clicked(object sender, EventArgs e) => _viewModel.SwapOriginDestinationSearch();
    }
}
