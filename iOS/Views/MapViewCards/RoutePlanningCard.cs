using System;
using UIKit;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class RoutePlanningCard : UIView
    {
        private MapViewModel _viewModel;

        private PseudoTextFieldButton _startTextPlaceholder;
        private PseudoTextFieldButton _endTextPlaceholder;

        private ActionButton _searchRouteButton;

        private UILabel _searchStartLabel;
        private UILabel _searchEndLabel;
        private UIButton _cancelRouteSearchButton;
        private UILabel _routeSearchHeader;
        private UIButton _swapOriginDestinationButton;

        internal RoutePlanningCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _startTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _endTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _searchRouteButton = new ActionButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _searchRouteButton.SetTitle("SearchForRouteButtonText".AsLocalized(), UIControlState.Normal);

            _cancelRouteSearchButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _searchStartLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "OriginRouteSearchFieldLabel".AsLocalized() };
            _searchEndLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "DestinationRouteSearchFieldLabel".AsLocalized() };

            _searchStartLabel.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);

            _routeSearchHeader = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "DirectionsPanelHeader".AsLocalized() };
            _routeSearchHeader.TextColor = ApplicationTheme.ForegroundColor;
            _routeSearchHeader.Font = ApplicationTheme.HeaderFont;

            // swap origin and destination button
            _swapOriginDestinationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _swapOriginDestinationButton.SetImage(UIImage.FromBundle("arrow-up-down"), UIControlState.Normal);
            _swapOriginDestinationButton.TintColor = ApplicationTheme.ForegroundColor;

            this.AddSubviews(_startTextPlaceholder, _endTextPlaceholder, _searchRouteButton, _searchStartLabel, _searchEndLabel, _cancelRouteSearchButton, _routeSearchHeader, _swapOriginDestinationButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // label
                _routeSearchHeader.TopAnchor.ConstraintEqualTo(this.TopAnchor, ApplicationTheme.Margin),
                _routeSearchHeader.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _routeSearchHeader.TrailingAnchor.ConstraintEqualTo(_cancelRouteSearchButton.LeadingAnchor, -ApplicationTheme.Margin),
                // close button
                _cancelRouteSearchButton.CenterYAnchor.ConstraintEqualTo(_routeSearchHeader.CenterYAnchor),
                _cancelRouteSearchButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _cancelRouteSearchButton.HeightAnchor.ConstraintEqualTo(32),
                _cancelRouteSearchButton.WidthAnchor.ConstraintEqualTo(32),
                // labels
                _searchStartLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _searchStartLabel.CenterYAnchor.ConstraintEqualTo(_startTextPlaceholder.CenterYAnchor),
                _searchEndLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _searchEndLabel.CenterYAnchor.ConstraintEqualTo(_endTextPlaceholder.CenterYAnchor),
                _searchEndLabel.TrailingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                // search bars
                _startTextPlaceholder.LeadingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor, ApplicationTheme.Margin),
                _startTextPlaceholder.TopAnchor.ConstraintEqualTo(_routeSearchHeader.BottomAnchor, ApplicationTheme.Margin),
                _startTextPlaceholder.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _startTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                _endTextPlaceholder.LeadingAnchor.ConstraintEqualTo(_startTextPlaceholder.LeadingAnchor),
                _endTextPlaceholder.TrailingAnchor.ConstraintEqualTo(_startTextPlaceholder.TrailingAnchor),
                _endTextPlaceholder.TopAnchor.ConstraintEqualTo(_startTextPlaceholder.BottomAnchor, ApplicationTheme.Margin),
                _endTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                // search button
                _searchRouteButton.TrailingAnchor.ConstraintEqualTo(_swapOriginDestinationButton.LeadingAnchor),
                _searchRouteButton.TopAnchor.ConstraintEqualTo(_endTextPlaceholder.BottomAnchor, ApplicationTheme.Margin),
                _searchRouteButton.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _searchRouteButton.HeightAnchor.ConstraintEqualTo(44),
                // swap origin and destinations button
                _swapOriginDestinationButton.HeightAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.WidthAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _swapOriginDestinationButton.CenterYAnchor.ConstraintEqualTo(_searchRouteButton.CenterYAnchor),
                // update bottom size
                this.BottomAnchor.ConstraintEqualTo(_searchRouteButton.BottomAnchor, ApplicationTheme.Margin)
            });

            _searchRouteButton.TouchUpInside += _searchRouteButton_TouchUpInside;
            _cancelRouteSearchButton.TouchUpInside += _cancelRouteSearchButton_TouchUpInside;
            _swapOriginDestinationButton.TouchUpInside += _swapOriginDestinationButton_TouchUpInside;
            _startTextPlaceholder.Tapped += originTapped;
            _endTextPlaceholder.Tapped += destinationTapped;

            _viewModel.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.OriginSearchText) || e.PropertyName == nameof(_viewModel.DestinationSearchText))
            {
                _startTextPlaceholder.Text = _viewModel.OriginSearchText;
                _endTextPlaceholder.Text = _viewModel.DestinationSearchText;
            }
        }

        private void originTapped(object sender, EventArgs e) => _viewModel.SelectOriginSearch();

        private void destinationTapped(object sender, EventArgs e) => _viewModel.SelectDestinationSearch();

        private void _searchRouteButton_TouchUpInside(object sender, EventArgs e) => _viewModel.PerformRouteSearch();

        private void _cancelRouteSearchButton_TouchUpInside(object sender, EventArgs e) => _viewModel.CancelRouteSearch();

        private void _swapOriginDestinationButton_TouchUpInside(object sender, EventArgs e) => _viewModel.SwapOriginDestinationSearch();
    }
}
