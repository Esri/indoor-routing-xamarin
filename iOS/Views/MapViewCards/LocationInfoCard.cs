using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class LocationInfoCard : UIView
    {
        private MapViewModel _viewModel;
        private ActionButton _startDirectionsButton;
        private CloseButton _closeButton;
        private UILabel _primaryLabel;
        private UILabel _secondaryLabel;

        internal LocationInfoCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _startDirectionsButton = new ActionButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _startDirectionsButton.SetTitle("FindDirectionsButtonText".AsLocalized(), UIControlState.Normal);

            _closeButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _primaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            _primaryLabel.Font = ApplicationTheme.HeaderFont;

            _secondaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            this.AddSubviews(_startDirectionsButton, _closeButton, _primaryLabel, _secondaryLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _primaryLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _primaryLabel.TopAnchor.ConstraintEqualTo(this.TopAnchor, ApplicationTheme.Margin),
                _primaryLabel.TrailingAnchor.ConstraintEqualTo(_closeButton.LeadingAnchor, -ApplicationTheme.Margin),
                //
                _secondaryLabel.LeadingAnchor.ConstraintEqualTo(_primaryLabel.LeadingAnchor),
                _secondaryLabel.TrailingAnchor.ConstraintEqualTo(_primaryLabel.TrailingAnchor),
                _secondaryLabel.TopAnchor.ConstraintEqualTo(_primaryLabel.BottomAnchor, ApplicationTheme.Margin),
                //
                _closeButton.TopAnchor.ConstraintEqualTo(this.TopAnchor, ApplicationTheme.Margin),
                _closeButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _closeButton.WidthAnchor.ConstraintEqualTo(32),
                _closeButton.HeightAnchor.ConstraintEqualTo(32),
                //
                _startDirectionsButton.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -ApplicationTheme.Margin),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_secondaryLabel.BottomAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_closeButton.BottomAnchor, ApplicationTheme.Margin),
                _startDirectionsButton.HeightAnchor.ConstraintEqualTo(44),
                //
                this.BottomAnchor.ConstraintEqualTo(_startDirectionsButton.BottomAnchor, ApplicationTheme.Margin)
            });

            // Handle closing location card.
            this._closeButton.TouchUpInside += Close_Clicked;

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
            if (e.PropertyName != nameof(_viewModel.CurrentlyIdentifiedRoom))
            {
                return;
            }

            _primaryLabel.Text = _viewModel.CurrentlyIdentifiedRoom?.RoomNumber ?? string.Empty;
            _secondaryLabel.Text = _viewModel.CurrentlyIdentifiedRoom?.EmployeeNameLabel ?? string.Empty;

            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString)"Room found".AsLocalized());
        }

        private void SearchDirections_Clicked(object sender, EventArgs e) => _viewModel.StartSearchFromFoundFeature();

        private void Close_Clicked(object sender, EventArgs e) => _viewModel.CloseLocationInfo();
    }
}
