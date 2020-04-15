using System;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class PseudoTextFieldButton : UIView
    {
        private readonly UILabel _label;
        private readonly UITapGestureRecognizer _tapRecognizer;
        private readonly UIVisualEffectView _backgroundView;

        private const int margin = 8;

        public string Text
        {
            get => _label?.Text ?? "";
            set
            {
                if (_label == null) { return; }

                if (value != _label.Text)
                {
                    _label.Text = value;
                }
            }
        }

        public PseudoTextFieldButton()
        {
            _backgroundView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.Prominent))
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            AddSubview(_backgroundView);

            _label = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LinkColor
            };

            _backgroundView.ContentView.AddSubview(_label);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _backgroundView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                _backgroundView.TopAnchor.ConstraintEqualTo(TopAnchor),
                _backgroundView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                _backgroundView.BottomAnchor.ConstraintEqualTo(BottomAnchor),
                _label.LeadingAnchor.ConstraintEqualTo(_backgroundView.LeadingAnchor, margin),
                _label.TopAnchor.ConstraintEqualTo(_backgroundView.TopAnchor, margin),
                _label.BottomAnchor.ConstraintEqualTo(_backgroundView.BottomAnchor, -margin),
                _label.TrailingAnchor.ConstraintEqualTo(_backgroundView.TrailingAnchor, -margin)
            });

            Layer.CornerRadius = 8;
            Layer.BorderColor = UIColor.SystemGrayColor.CGColor;
            Layer.BorderWidth = 2;
            UserInteractionEnabled = true; //for gesture recognizer
            
            BackgroundColor = UIColor.Clear;
            ClipsToBounds = true;

            _tapRecognizer = new UITapGestureRecognizer(HandleTap);
            AddGestureRecognizer(_tapRecognizer);
        }

        private void HandleTap()
        {
            Tapped?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Tapped;
    }
}
