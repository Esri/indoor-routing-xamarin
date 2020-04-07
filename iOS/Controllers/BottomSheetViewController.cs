using System;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public class BottomSheetViewController : UIViewController
    {
        public enum BottomSheetState
        {
            minimized,
            partial,
            full
        };

        private BottomSheetState _currentState = BottomSheetState.minimized;

        private UIPanGestureRecognizer _gesture;
        private UIView _containerView;
        private nfloat minHeight = 76;

        private UIView _handlebar;
        private UIView _handlebarSeparator;

        private UIColor _handlebarColor = UIColor.SystemGray2Color;

        private UIView _blurShadowContainerView;

        public nfloat partialHeight = 160;

        public BottomSheetViewController(UIView container)
        {
            _containerView = container; // TODO - is there a better way?
            _gesture = new UIPanGestureRecognizer(HandleMoveView);

            var blurView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial))
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ClipsToBounds = true
            };

            // Defined in Helpers/ViewExtensions
            _blurShadowContainerView = blurView.EncapsulateInShadowView();

            View = _blurShadowContainerView;
            _containerView.AddSubview(View);

            DisplayedContentView.BackgroundColor = UIColor.Clear;
            DisplayedContentView.ClipsToBounds = true;

            blurView.ContentView.AddSubview(DisplayedContentView);

            _handlebar = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            _handlebar.Layer.CornerRadius = 2;
            _handlebar.BackgroundColor = _handlebarColor;
            blurView.ContentView.AddSubview(_handlebar);

            _handlebarSeparator = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            _handlebarSeparator.BackgroundColor = _handlebarColor;
            blurView.ContentView.AddSubview(_handlebarSeparator);

            blurView.AddGestureRecognizer(_gesture);
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _handlebar.WidthAnchor.ConstraintEqualTo(36),
                _handlebar.CenterXAnchor.ConstraintEqualTo(blurView.CenterXAnchor),
                _handlebar.HeightAnchor.ConstraintEqualTo(4),
                _handlebar.WidthAnchor.ConstraintEqualTo(48),
                _handlebarSeparator.HeightAnchor.ConstraintEqualTo(0.5f),
                _handlebarSeparator.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                _handlebarSeparator.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor)
            });
            

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                DisplayedContentView.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                DisplayedContentView.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor),
                blurView.TopAnchor.ConstraintGreaterThanOrEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor)
            });

            const int handleBarToSeparatorMargin = 4;

            _regularWidthConstraints = new[]
            {
                _blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, 16),
                _blurShadowContainerView.WidthAnchor.ConstraintEqualTo(320),
                _blurShadowContainerView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, 16),
                _blurShadowContainerView.BottomAnchor.ConstraintGreaterThanOrEqualTo(_blurShadowContainerView.TopAnchor, 44),
                _blurShadowContainerView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                _handlebar.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor, -4),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebarSeparator.TopAnchor, -8),
                _handlebarSeparator.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -handleBarToSeparatorMargin)
            };

            _compactWidthConstraints = new[]
            {
                _blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                _blurShadowContainerView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                _blurShadowContainerView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, 8), // TODO find another way to correct for bottom radius
                _handlebar.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor, 8),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor),
                _handlebarSeparator.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor, handleBarToSeparatorMargin)
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
                // translate height constraint
                _heightConstraint.Constant += translation.Y;
            }
            else
            {
                // translate height constraint
                _heightConstraint.Constant -= translation.Y;
            }

            if (_heightConstraint.Constant > MaxHeightConstraint)
            {
                _heightConstraint.Constant = MaxHeightConstraint;
            }

            // handle going past limit (animation effect)
            if (_heightConstraint.Constant < minHeight)
            {
                _heightConstraint.Constant = minHeight;
            }

            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                if (Math.Abs(recognizer.VelocityInView(View).Y) > 0)
                {
                    AnimateSwitchState(recognizer);
                }

                if (_heightConstraint.Constant == minHeight)
                {
                    _currentState = BottomSheetState.minimized;
                } else if (_heightConstraint.Constant == MaxHeightConstraint)
                {
                    _currentState = BottomSheetState.full;
                }
                else
                {
                    _currentState = BottomSheetState.partial;
                }
            }

            recognizer.SetTranslation(new CoreGraphics.CGPoint(0, 0), View);
        }

        private nfloat MaxHeightConstraint
        {
            get
            {
                switch (TraitCollection.HorizontalSizeClass)
                {
                    case UIUserInterfaceSizeClass.Compact:
                        return _containerView.Frame.Height + 8 - _containerView.SafeAreaInsets.Top - 16; //16 is top margin
                    case UIUserInterfaceSizeClass.Regular:
                    default:
                        return _containerView.Frame.Height - _containerView.SafeAreaInsets.Top - 16 - _containerView.SafeAreaInsets.Bottom - 16; //16 is bottom margin
                }
            }
        }

        private nfloat GetPartialHeight()
        {
            nfloat baseHeight = 0;
            if (DisplayedContentView.Subviews.First() is IntrinsicContentSizedStackView stackView)
            {
                baseHeight = stackView.SystemLayoutSizeFittingSize(new CoreGraphics.CGSize(-1, -1)).Height;
            }
            else
            {
                baseHeight = partialHeight;
            }

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact)
            {
                baseHeight += 12.5f + 8f; // 8f is extra margin, 12.5 is size of UI elements

                // account for bottom safe area
                baseHeight += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
            }
            else
            {
                baseHeight += 12.5f;
            }
            return baseHeight;
        }

        public void SetStateWithAnimation(BottomSheetState state)
        {
            _currentState = state;
            switch (state)
            {
                case BottomSheetState.partial:
                    UIView.Animate(0.5, () =>
                    {
                        _heightConstraint.Constant = GetPartialHeight();
                    });
                    break;
                case BottomSheetState.minimized:
                    UIView.Animate(0.5, () =>
                    {
                        _heightConstraint.Constant = minHeight;
                    });
                    break;
                case BottomSheetState.full:
                    UIView.Animate(0.5, () =>
                    {
                        _heightConstraint.Constant = MaxHeightConstraint;
                    });
                    break;
            }
        }

        private void AnimateSwitchState(UIPanGestureRecognizer recognizer)
        {
            switch (_currentState)
            {
                case BottomSheetState.minimized:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.partial);
                    }
                    break;
                case BottomSheetState.partial:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.full);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.full);
                    }
                    break;
                case BottomSheetState.full:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.partial);
                    }
                    break;
            }
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
                _handlebarSeparator.BackgroundColor = _handlebarColor;
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_compactWidthConstraints);
                _handlebarSeparator.BackgroundColor = UIColor.Clear;
            }

            SetStateWithAnimation(_currentState);
        }
    }
}