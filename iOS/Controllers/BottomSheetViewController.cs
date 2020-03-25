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
        private nfloat minHeight = 76;

        private UIView _handlebar;

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
            _containerView.AddSubview(View);
            View.BackgroundColor = UIColor.Clear;

            DisplayedContentView.BackgroundColor = UIColor.Clear;

            blurView.ContentView.AddSubview(DisplayedContentView);

            _handlebar = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            _handlebar.Layer.CornerRadius = 2;
            _handlebar.BackgroundColor = UIColor.SystemGray2Color;
            blurView.ContentView.AddSubview(_handlebar);

            blurView.AddGestureRecognizer(_gesture);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _handlebar.WidthAnchor.ConstraintEqualTo(36),
                _handlebar.CenterXAnchor.ConstraintEqualTo(blurView.CenterXAnchor),
                _handlebar.HeightAnchor.ConstraintEqualTo(4)
            });
            

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                DisplayedContentView.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                DisplayedContentView.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor),
                blurView.TopAnchor.ConstraintGreaterThanOrEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor)
            });


            _regularWidthConstraints = new[]
            {
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, 16),
                blurView.WidthAnchor.ConstraintEqualTo(320),
                blurView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, 16),
                blurView.BottomAnchor.ConstraintGreaterThanOrEqualTo(blurView.TopAnchor, 44),
                blurView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                _handlebar.BottomAnchor.ConstraintEqualTo(blurView.BottomAnchor, -8),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(blurView.TopAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -8)
            };

            _compactWidthConstraints = new[]
            {
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                blurView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                blurView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, 8), // TODO find another way to correct for bottom radius
                _handlebar.TopAnchor.ConstraintEqualTo(blurView.TopAnchor, 8),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(blurView.BottomAnchor)
            };

            _heightConstraint = View.HeightAnchor.ConstraintEqualTo(150);
            _heightConstraint.Active = true;

            ApplyConstraints();
        }

        private void HandleMoveView(UIPanGestureRecognizer recognizer)
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

            // handle going past limit (animation effect)
            if (_heightConstraint.Constant < minHeight)
            {
                _heightConstraint.Constant = minHeight;
            }

            recognizer.SetTranslation(new CoreGraphics.CGPoint(0, 0), View);
        }

        private nfloat logConstraintForHeight(nfloat constant)
        {
            return constant;
        }

        private void AnimateSnapToLimit()
        {
            UIView.Animate(1, 0, UIViewAnimationOptions.CurveEaseIn, () =>
            {
                _heightConstraint.Constant = minHeight;
            }, null);
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