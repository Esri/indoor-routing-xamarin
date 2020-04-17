using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
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
                Font = ApplicationTheme.HeaderFont,
                TextColor = ApplicationTheme.ForegroundColor,
                Text = "NotFoundTitle".AsLocalized()
            };

            _errorTextView = new UITextView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = ApplicationTheme.ForegroundColor,
                Text = "NotFoundCardMessage".AsLocalized()
            };

            _dismissButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            AddSubviews(_headerLabel, _errorTextView, _dismissButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _headerLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, ApplicationTheme.Margin),
                _headerLabel.CenterYAnchor.ConstraintEqualTo(_dismissButton.CenterYAnchor),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(_dismissButton.LeadingAnchor, -ApplicationTheme.Margin),
                _dismissButton.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                _dismissButton.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                _dismissButton.WidthAnchor.ConstraintEqualTo(32),
                _dismissButton.HeightAnchor.ConstraintEqualTo(32),
                _errorTextView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _errorTextView.TopAnchor.ConstraintEqualTo(_dismissButton.BottomAnchor, ApplicationTheme.Margin),
                _errorTextView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                BottomAnchor.ConstraintEqualTo(_errorTextView.BottomAnchor, ApplicationTheme.Margin)
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

            if (_viewModel.CurrentState == UIState.RouteNotFound)
            {
                _headerLabel.Text = "RouteNotFoundCardTitle".AsLocalized();
            }
            else if (_viewModel.CurrentState == UIState.LocationNotFound)
            {
                _headerLabel.Text = "LocationNotFoundCardTitle".AsLocalized();
            }
        }

        private void Dismiss_Clicked(object sender, EventArgs e) => _viewModel.DismissNotFound();
    }
}
