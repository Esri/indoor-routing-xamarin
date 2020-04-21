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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    public sealed class RouteResultCard : UIView
    {
        private readonly MapViewModel _viewModel;

        private readonly SelfSizedTableView _stopsTable;
        private readonly UILabel _routeDurationLabel;

        internal RouteResultCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _stopsTable = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                //TableFooterView = null,
                ScrollEnabled = false,
                BackgroundColor = UIColor.Clear,
                AllowsSelection = false
            };

            // Future - consider supporting more travel modes?
            var travelModeImageView = new UIImageView(UIImage.FromBundle("walking"))
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TintColor = UIColor.LabelColor,
                ContentMode = UIViewContentMode.ScaleAspectFit
            };

            _routeDurationLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };

            UIButton closeButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            var headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Text = "RouteResultHeader".Localize(),
                Font = ApplicationTheme.HeaderFont,
                TextColor = ApplicationTheme.ForegroundColor
            };

            AddSubviews(_stopsTable, travelModeImageView, _routeDurationLabel, closeButton, headerLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // result header
                headerLabel.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                headerLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                headerLabel.TrailingAnchor.ConstraintEqualTo(closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                //clear button
                closeButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                closeButton.CenterYAnchor.ConstraintEqualTo(headerLabel.CenterYAnchor),
                closeButton.WidthAnchor.ConstraintEqualTo(32),
                closeButton.HeightAnchor.ConstraintEqualTo(32),
                // stops view
                _stopsTable.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                _stopsTable.TopAnchor.ConstraintEqualTo(_routeDurationLabel.BottomAnchor, ApplicationTheme.Margin),
                _stopsTable.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                // image
                travelModeImageView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                travelModeImageView.TopAnchor.ConstraintEqualTo(_routeDurationLabel.TopAnchor),
                travelModeImageView.BottomAnchor.ConstraintEqualTo(_routeDurationLabel.BottomAnchor),
                travelModeImageView.WidthAnchor.ConstraintEqualTo(32),
                // walk time label
                _routeDurationLabel.TopAnchor.ConstraintEqualTo(headerLabel.BottomAnchor, ApplicationTheme.Margin),
                _routeDurationLabel.LeadingAnchor.ConstraintEqualTo(travelModeImageView.TrailingAnchor, ApplicationTheme.Margin),
                //
                BottomAnchor.ConstraintEqualTo(_stopsTable.BottomAnchor, ApplicationTheme.Margin)
            });


            closeButton.TouchUpInside += Close_Clicked;

            _viewModel.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentRoute))
            {
                return;
            }

            RouteResult routeResult = _viewModel.CurrentRoute;

            if (routeResult == null)
            {
                _stopsTable.Source = null;
                return;
            }

            StringBuilder walkTimeStringBuilder = new StringBuilder();

            Route firstRoute = routeResult.Routes.First();

            // Add walk time and distance label
            if (firstRoute.TotalTime.Hours > 0)
            {
                walkTimeStringBuilder.Append($"{firstRoute.TotalTime.Hours} h {firstRoute.TotalTime.Minutes} m");
            }
            else
            {
                walkTimeStringBuilder.Append($"{firstRoute.TotalTime.Minutes + 1} min");
            }

            var tableSource = new List<Feature> { _viewModel.FromLocationFeature, _viewModel.ToLocationFeature };

            _stopsTable.Source = new RouteTableSource(tableSource);

            // necessary so that table is reloaded before layout is requested
            _stopsTable.ReloadData();

            _routeDurationLabel.Text = walkTimeStringBuilder.ToString();

            RelayoutRequested?.Invoke(this, EventArgs.Empty);

            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"Route found".Localize());
        }

        private void Close_Clicked(object sender, EventArgs e) => _viewModel.CloseRouteResult();

        public event EventHandler RelayoutRequested;
    }
}
