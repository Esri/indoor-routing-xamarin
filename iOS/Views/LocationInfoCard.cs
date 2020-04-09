using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class LocationInfoCard : UIView
    {
        private UIButton _startDirectionsButton;
        private CloseButton _closeButton;
        private UILabel _primaryLabel;
        private UILabel _secondaryLabel;

        public LocationInfoCard()
        {
            _startDirectionsButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _startDirectionsButton.SetTitle("FindDirectionsButtonText".AsLocalized(), UIControlState.Normal);
            _startDirectionsButton.BackgroundColor = UIColor.SystemBlueColor;
            _startDirectionsButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _startDirectionsButton.SetTitleColor(UIColor.SystemGrayColor, UIControlState.Disabled);
            _startDirectionsButton.Layer.CornerRadius = 8;

            // Handle searching for directions
            _startDirectionsButton.TouchUpInside += SearchDirections_Clicked; ;

            _closeButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };


            _primaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            _primaryLabel.Font = _primaryLabel.Font.WithSize(18);

            _secondaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            this.AddSubviews(_startDirectionsButton, _closeButton, _primaryLabel, _secondaryLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _primaryLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _primaryLabel.TopAnchor.ConstraintEqualTo(this.TopAnchor, 8),
                _primaryLabel.TrailingAnchor.ConstraintEqualTo(_closeButton.LeadingAnchor, -8),
                //
                _secondaryLabel.LeadingAnchor.ConstraintEqualTo(_primaryLabel.LeadingAnchor),
                _secondaryLabel.TrailingAnchor.ConstraintEqualTo(_primaryLabel.TrailingAnchor),
                _secondaryLabel.TopAnchor.ConstraintEqualTo(_primaryLabel.BottomAnchor, 8),
                //
                _closeButton.TopAnchor.ConstraintEqualTo(this.TopAnchor, 8),
                _closeButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                _closeButton.WidthAnchor.ConstraintEqualTo(32),
                _closeButton.HeightAnchor.ConstraintEqualTo(32),
                //
                _startDirectionsButton.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _startDirectionsButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_secondaryLabel.BottomAnchor, 8),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_closeButton.BottomAnchor, 8),
                _startDirectionsButton.HeightAnchor.ConstraintEqualTo(44),
                //
                this.BottomAnchor.ConstraintEqualTo(_startDirectionsButton.BottomAnchor, 8)
            });

            // Handle closing location card.
            this._closeButton.TouchUpInside += Close_Clicked;

            // Check settings
            _startDirectionsButton.Enabled = AppSettings.CurrentSettings.IsRoutingEnabled;

            // Wait for app state changes (particularly identify feature results)
            AppStateViewModel.Instance.PropertyChanged += ViewModel_Changed;
        }

        private void ViewModel_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AppStateViewModel.Instance.CurrentlyIdentifiedRoom))
            {
                return;
            }

            _primaryLabel.Text = AppStateViewModel.Instance.CurrentlyIdentifiedRoom?.RoomNumber ?? string.Empty;
            _secondaryLabel.Text = AppStateViewModel.Instance.CurrentlyIdentifiedRoom?.EmployeeNameLabel ?? string.Empty;
        }

        private void SearchDirections_Clicked(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);

        private void Close_Clicked(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.AwaitingSearch);
    }
}
