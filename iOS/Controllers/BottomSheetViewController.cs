using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public class BottomSheetViewController : UIViewController
    {
        private enum State
        {
            minimized,
            partial,
            full
        };

        private UIPanGestureRecognizer _gesture;
        private UIView _containerView;
        private nfloat minHeight = 80;

        public BottomSheetViewController(UIView container)
        {
            _containerView = container; // TODO - is there a better way?
            _gesture = new UIPanGestureRecognizer(HandleMoveView);

            var blurView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial))
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ClipsToBounds = true
            };
            blurView.Layer.CornerRadius = 8;

            View = blurView;

            blurView.AddGestureRecognizer(_gesture);

            _containerView.AddSubview(View);
            View.BackgroundColor = UIColor.Clear;

            DisplayedContentView.BackgroundColor = UIColor.Clear;

            blurView.ContentView.AddSubview(DisplayedContentView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                DisplayedContentView.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                DisplayedContentView.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(blurView.TopAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(blurView.BottomAnchor)
            });


            _regularWidthConstraints = new[]
            {
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, 16),
                blurView.WidthAnchor.ConstraintEqualTo(320),
                blurView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, 16)
            };

            _compactWidthConstraints = new[]
            {
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                blurView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                blurView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, 8), // TODO find another way to correct for bottom radius
            };

            _heightConstraint = View.HeightAnchor.ConstraintEqualTo(150);
            _heightConstraint.Active = true;

            ApplyConstraints();
        }

        private void HandleMoveView(UIPanGestureRecognizer recognizer)
        {
            MoveView(recognizer);
            
            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                UIView.Animate(1, () =>
                {
                    if (_heightConstraint.Constant < minHeight)
                    {
                        _heightConstraint.Constant = minHeight;
                    }
                });
            }
        }

        private void MoveView(UIPanGestureRecognizer recognizer)
        {
            var translation = recognizer.TranslationInView(View);
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                _heightConstraint.Constant += translation.Y;
            }
            else
            {
                _heightConstraint.Constant -= translation.Y;
            }

            if (_heightConstraint.Constant < minHeight)
            {
                _heightConstraint.Constant = minHeight;
            }
            
            recognizer.SetTranslation(new CoreGraphics.CGPoint(0, 0), View);
        }

        public UIView DisplayedContentView { get;  } = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);
            ApplyConstraints();
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                _heightConstraint.Constant = 320;
            }
            else
            {
                _heightConstraint.Constant = minHeight;
            }
        }

        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint _heightConstraint;

        private void ApplyConstraints()
        {
            NSLayoutConstraint.DeactivateConstraints(_regularWidthConstraints);
            NSLayoutConstraint.DeactivateConstraints(_compactWidthConstraints);
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_regularWidthConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_compactWidthConstraints);
            }
        }
    }
}