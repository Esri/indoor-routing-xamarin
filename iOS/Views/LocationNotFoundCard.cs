using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class LocationNotFoundCard : UIView
    {
        private UILabel _headerLabel;
        private UITextView _errorTextView;
        private CloseButton _dismissButton;

        public LocationNotFoundCard()
        {
            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = UIFont.BoldSystemFontOfSize(28),
                TextColor = UIColor.LabelColor,
                Text = "Location not found"
            };

            _errorTextView = new UITextView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };// TODO - ever set this

            _dismissButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            this.AddSubviews(_headerLabel, _errorTextView, _dismissButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _headerLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _headerLabel.CenterYAnchor.ConstraintEqualTo(_dismissButton.CenterYAnchor),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(_dismissButton.LeadingAnchor, -8),
                _dismissButton.TopAnchor.ConstraintEqualTo(TopAnchor, 8),
                _dismissButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -8),
                _dismissButton.WidthAnchor.ConstraintEqualTo(32),
                _dismissButton.HeightAnchor.ConstraintEqualTo(32),
                _errorTextView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, 8),
                _errorTextView.TopAnchor.ConstraintEqualTo(_dismissButton.BottomAnchor, 8),
                _errorTextView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -8),
                BottomAnchor.ConstraintEqualTo(_errorTextView.BottomAnchor, 8)
            });

            _dismissButton.TouchUpInside += Dismiss_Clicked;
        }

        private void Dismiss_Clicked(object sender, EventArgs e)
        {
            if (AppStateViewModel.Instance.CurrentState == AppStateViewModel.UIState.LocationNotFound)
            {
                AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.ReadyWaiting);
            }
            else
            {
                AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);
            }
        }
    }
}
