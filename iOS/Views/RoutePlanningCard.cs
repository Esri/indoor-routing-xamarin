using System;
using UIKit;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Foundation;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class RoutePlanningCard : UIView
    {
        private PseudoTextFieldButton _startTextPlaceholder;
        private PseudoTextFieldButton _endTextPlaceholder;

        private UIButton _searchRouteButton;

        private UILabel _searchStartLabel;
        private UILabel _searchEndLabel;
        private UIButton _cancelRouteSearchButton;
        private UILabel _routeSearchHeader;
        private UIButton _swapOriginDestinationButton;

        public RoutePlanningCard()
        {
            _startTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _endTextPlaceholder = new PseudoTextFieldButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _searchRouteButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _searchRouteButton.SetTitle("SearchForRouteButtonText".AsLocalized(), UIControlState.Normal);
            _searchRouteButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _searchRouteButton.BackgroundColor = UIColor.SystemBlueColor;
            _searchRouteButton.Layer.CornerRadius = 8;


            _cancelRouteSearchButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _searchStartLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "OriginRouteSearchFieldLabel".AsLocalized() };
            _searchEndLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "DestinationRouteSearchFieldLabel".AsLocalized() };

            _routeSearchHeader = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "DirectionsPanelHeader".AsLocalized() };
            _routeSearchHeader.TextColor = UIColor.LabelColor;
            _routeSearchHeader.Font = UIFont.BoldSystemFontOfSize(24);

            // swap origin and destination button
            _swapOriginDestinationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _swapOriginDestinationButton.SetImage(UIImage.FromBundle("arrow-up-down"), UIControlState.Normal);
            _swapOriginDestinationButton.TintColor = UIColor.LabelColor;

            this.AddSubviews(_startTextPlaceholder, _endTextPlaceholder, _searchRouteButton, _searchStartLabel, _searchEndLabel, _cancelRouteSearchButton, _routeSearchHeader, _swapOriginDestinationButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // label
                _routeSearchHeader.TopAnchor.ConstraintEqualTo(this.TopAnchor, 8),
                _routeSearchHeader.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _routeSearchHeader.TrailingAnchor.ConstraintEqualTo(_cancelRouteSearchButton.LeadingAnchor, -8),
                // close button
                _cancelRouteSearchButton.CenterYAnchor.ConstraintEqualTo(_routeSearchHeader.CenterYAnchor),
                _cancelRouteSearchButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                _cancelRouteSearchButton.HeightAnchor.ConstraintEqualTo(32),
                _cancelRouteSearchButton.WidthAnchor.ConstraintEqualTo(32),
                // labels
                _searchStartLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _searchStartLabel.CenterYAnchor.ConstraintEqualTo(_startTextPlaceholder.CenterYAnchor),
                _searchEndLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _searchEndLabel.CenterYAnchor.ConstraintEqualTo(_endTextPlaceholder.CenterYAnchor),
                _searchEndLabel.TrailingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                // search bars
                _startTextPlaceholder.LeadingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor, 8),
                _startTextPlaceholder.TopAnchor.ConstraintEqualTo(_routeSearchHeader.BottomAnchor, 8),
                _startTextPlaceholder.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                _startTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                _endTextPlaceholder.LeadingAnchor.ConstraintEqualTo(_startTextPlaceholder.LeadingAnchor),
                _endTextPlaceholder.TrailingAnchor.ConstraintEqualTo(_startTextPlaceholder.TrailingAnchor),
                _endTextPlaceholder.TopAnchor.ConstraintEqualTo(_startTextPlaceholder.BottomAnchor, 8),
                _endTextPlaceholder.HeightAnchor.ConstraintEqualTo(44),
                // search button
                _searchRouteButton.TrailingAnchor.ConstraintEqualTo(_swapOriginDestinationButton.LeadingAnchor),
                _searchRouteButton.TopAnchor.ConstraintEqualTo(_endTextPlaceholder.BottomAnchor, 8),
                _searchRouteButton.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _searchRouteButton.HeightAnchor.ConstraintEqualTo(44),
                // swap origin and destinations button
                _swapOriginDestinationButton.HeightAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.WidthAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                _swapOriginDestinationButton.CenterYAnchor.ConstraintEqualTo(_searchRouteButton.CenterYAnchor),
                // update bottom size
                this.BottomAnchor.ConstraintEqualTo(_searchRouteButton.BottomAnchor, 8)
            });

            _searchRouteButton.TouchUpInside += _searchRouteButton_TouchUpInside;
            _cancelRouteSearchButton.TouchUpInside += _cancelRouteSearchButton_TouchUpInside;
            _swapOriginDestinationButton.TouchUpInside += _swapOriginDestinationButton_TouchUpInside;
            _startTextPlaceholder.Tapped += originTapped;
            _endTextPlaceholder.Tapped += destinationTapped;

            AppStateViewModel.Instance.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppStateViewModel.OriginSearchText) || e.PropertyName == nameof(AppStateViewModel.DestinationSearchText))
            {
                _startTextPlaceholder.Text = AppStateViewModel.Instance.OriginSearchText;
                _endTextPlaceholder.Text = AppStateViewModel.Instance.DestinationSearchText;
            }
        }

        private void originTapped(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchingForOrigin);

        private void destinationTapped(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchingForDestination);

        private void _searchRouteButton_TouchUpInside(object sender, EventArgs e) => AppStateViewModel.Instance.PerformRouteSearchAndUpdateState();

        private void _cancelRouteSearchButton_TouchUpInside(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationFound);

        private void _swapOriginDestinationButton_TouchUpInside(object sender, EventArgs e)
        {
            string oldOriginSearch = AppStateViewModel.Instance.OriginSearchText;
            string oldDestinationSearch = AppStateViewModel.Instance.DestinationSearchText;

            AppStateViewModel.Instance.DestinationSearchText = oldOriginSearch;
            AppStateViewModel.Instance.OriginSearchText = oldDestinationSearch;
        }
    }
}
