using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class RouteResultCard : UIView
    {
        private MapViewModel _viewModel;

        private SelfSizedTableView _stopsTable;
        private UIImageView _travelModeImageView;
        private UILabel _routeDurationLabel;
        private UIButton _closeButton;
        private UILabel _headerLabel;

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
            _travelModeImageView = new UIImageView(UIImage.FromBundle("walking")) { TranslatesAutoresizingMaskIntoConstraints = false };
            _travelModeImageView.TintColor = UIColor.LabelColor;
            _travelModeImageView.ContentMode = UIViewContentMode.ScaleAspectFit;

            _routeDurationLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };

            _closeButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _headerLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "RouteResultHeader".AsLocalized() };
            _headerLabel.Font = ApplicationTheme.HeaderFont;
            _headerLabel.TextColor = ApplicationTheme.ForegroundColor;

            this.AddSubviews(_stopsTable, _travelModeImageView, _routeDurationLabel, _closeButton, _headerLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // result header
                _headerLabel.TopAnchor.ConstraintEqualTo(this.TopAnchor, ApplicationTheme.Margin),
                _headerLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(_closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                //clear button
                _closeButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _closeButton.CenterYAnchor.ConstraintEqualTo(_headerLabel.CenterYAnchor),
                _closeButton.WidthAnchor.ConstraintEqualTo(32),
                _closeButton.HeightAnchor.ConstraintEqualTo(32),
                // stops view
                _stopsTable.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor),
                _stopsTable.TopAnchor.ConstraintEqualTo(_routeDurationLabel.BottomAnchor, ApplicationTheme.Margin),
                _stopsTable.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                // image
                _travelModeImageView.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _travelModeImageView.TopAnchor.ConstraintEqualTo(_routeDurationLabel.TopAnchor),
                _travelModeImageView.BottomAnchor.ConstraintEqualTo(_routeDurationLabel.BottomAnchor),
                _travelModeImageView.WidthAnchor.ConstraintEqualTo(32),
                // walk time label
                _routeDurationLabel.TopAnchor.ConstraintEqualTo(_headerLabel.BottomAnchor, ApplicationTheme.Margin),
                _routeDurationLabel.LeadingAnchor.ConstraintEqualTo(_travelModeImageView.TrailingAnchor, ApplicationTheme.Margin),
                //
                this.BottomAnchor.ConstraintEqualTo(_stopsTable.BottomAnchor, ApplicationTheme.Margin)
            });


            _closeButton.TouchUpInside += Close_Clicked;

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

            Route firstReoute = routeResult.Routes.First();

            // Add walk time and distance label
            if (firstReoute.TotalTime.Hours > 0)
            {
                walkTimeStringBuilder.Append(string.Format("{0} h {1} m", firstReoute.TotalTime.Hours, firstReoute.TotalTime.Minutes));
            }
            else
            {
                walkTimeStringBuilder.Append(string.Format("{0} min", firstReoute.TotalTime.Minutes + 1));
            }

            var tableSource = new List<Feature>() { _viewModel.FromLocationFeature, _viewModel.ToLocationFeature };

            _stopsTable.Source = new RouteTableSource(tableSource);

            // necessary so that table is reloaded before layout is requested
            _stopsTable.ReloadData();

            _routeDurationLabel.Text = walkTimeStringBuilder.ToString();

            RelayoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Clicked(object sender, EventArgs e) => _viewModel.CloseRouteResult();

        public event EventHandler RelayoutRequested;
    }
}
