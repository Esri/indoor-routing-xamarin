using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class NotFoundCard : UIView
    {
        private MapViewModel _viewModel;

        private UILabel _headerLabel;
        private UITextView _errorTextView;
        private CloseButton _dismissButton;

        internal NotFoundCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = UIFont.BoldSystemFontOfSize(28),
                TextColor = UIColor.LabelColor,
                Text = "Not Found"
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

            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }

            if (_viewModel.CurrentState == MapViewModel.UIState.RouteNotFound)
            {
                _headerLabel.Text = "Route not found";
            }
            else if (_viewModel.CurrentState == MapViewModel.UIState.LocationNotFound)
            {
                _headerLabel.Text = "Location not found";
            }
        }

        private void Dismiss_Clicked(object sender, EventArgs e) => _viewModel.DismissNotFound();
    }
}
