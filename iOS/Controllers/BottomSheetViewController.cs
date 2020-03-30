using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
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

        private State _currentState = State.minimized;

        private UIPanGestureRecognizer _gesture;
        private UIView _containerView;
        private nfloat minHeight = 76;

        private UIView _handlebar;
        private UIView _handlebarSeparator;

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

            _handlebarSeparator = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
            _handlebarSeparator.BackgroundColor = UIColor.SystemGray2Color;
            blurView.ContentView.AddSubview(_handlebarSeparator);

            blurView.AddGestureRecognizer(_gesture);

            // Defined in Helpers/ViewExtensions
            blurView.ApplyStandardShadow();

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
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, 16),
                blurView.WidthAnchor.ConstraintEqualTo(320),
                blurView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, 16),
                blurView.BottomAnchor.ConstraintGreaterThanOrEqualTo(blurView.TopAnchor, 44),
                blurView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                _handlebar.BottomAnchor.ConstraintEqualTo(blurView.BottomAnchor, -4),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(blurView.TopAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebarSeparator.TopAnchor, -8),
                _handlebarSeparator.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -handleBarToSeparatorMargin)
            };

            _compactWidthConstraints = new[]
            {
                blurView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                blurView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                blurView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, 8), // TODO find another way to correct for bottom radius
                _handlebar.TopAnchor.ConstraintEqualTo(blurView.TopAnchor, 8),
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(blurView.BottomAnchor),
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
                    _currentState = State.minimized;
                } else if (_heightConstraint.Constant == MaxHeightConstraint)
                {
                    _currentState = State.full;
                }
                else
                {
                    _currentState = State.partial;
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
                        return _containerView.Frame.Height + 8 - _containerView.SafeAreaInsets.Top;
                    case UIUserInterfaceSizeClass.Regular:
                    default:
                        return _containerView.Frame.Height - _containerView.SafeAreaInsets.Top - 16 - _containerView.SafeAreaInsets.Bottom;
                }
            }
        }

        private void AnimateSwitchState(UIPanGestureRecognizer recognizer)
        {
            switch (_currentState)
            {
                case State.minimized:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        _currentState = State.partial;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = partialHeight;
                        });
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        _currentState = State.partial;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = partialHeight;
                        });
                    }
                    break;
                case State.partial:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        _currentState = State.full;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = MaxHeightConstraint;
                        });
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        _currentState = State.minimized;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = minHeight;
                        });
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        _currentState = State.minimized;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = minHeight;
                        });
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        _currentState = State.full;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = MaxHeightConstraint;
                        });
                    }
                    break;
                case State.full:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        _currentState = State.partial;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = partialHeight;
                        });
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        _currentState = State.partial;
                        UIView.Animate(0.5, () =>
                        {
                            _heightConstraint.Constant = partialHeight;
                        });
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