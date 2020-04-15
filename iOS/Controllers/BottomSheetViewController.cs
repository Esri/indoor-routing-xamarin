﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        private BottomSheetState _currentState = BottomSheetState.partial;

        private UIPanGestureRecognizer _gesture;
        private UIView _containerView;
        private nfloat minHeight = 76;

        private UIView _handlebar;
        private UIView _handlebarSeparator;

        private UIColor _handlebarColor = UIColor.SystemGray2Color;

        private UIView _blurShadowContainerView;

        public nfloat partialHeight = 160;

        public NSLayoutYAxisAnchor PanelTopAnchor { get; set; }

        public bool AllowsMinimumHeight { get; set; } = false;

        public bool AllowsManualResize { get; set; } = false;

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

            if (AllowsManualResize)
            {
                _handlebar = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
                _handlebar.Layer.CornerRadius = 2;
                _handlebar.BackgroundColor = _handlebarColor;
                blurView.ContentView.AddSubview(_handlebar);

                _handlebarSeparator = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
                _handlebarSeparator.BackgroundColor = _handlebarColor;
                blurView.ContentView.AddSubview(_handlebarSeparator);

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

                blurView.AddGestureRecognizer(_gesture);


            }

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                DisplayedContentView.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                DisplayedContentView.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor),
                blurView.TopAnchor.ConstraintGreaterThanOrEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor)
            });

            const int handleBarToSeparatorMargin = 4;

            var regularWidthConstraints = new List<NSLayoutConstraint>()
            {
                _blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, 16),
                _blurShadowContainerView.WidthAnchor.ConstraintEqualTo(320),
                _blurShadowContainerView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, 16),
                _blurShadowContainerView.BottomAnchor.ConstraintGreaterThanOrEqualTo(_blurShadowContainerView.TopAnchor, 44),
                _blurShadowContainerView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor),
                
            };

            if (AllowsManualResize)
            {
                regularWidthConstraints.Add(_handlebar.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor, -4));
                regularWidthConstraints.Add(DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebarSeparator.TopAnchor, -8));
                regularWidthConstraints.Add(_handlebarSeparator.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -handleBarToSeparatorMargin));
            }
            else
            {
                regularWidthConstraints.Add(DisplayedContentView.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor));
            }

            _regularWidthConstraints = regularWidthConstraints.ToArray();

            var compactWidthConstraints = new List<NSLayoutConstraint>
            {
                _blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                _blurShadowContainerView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                _blurShadowContainerView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, 8),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor)
            };

            if (AllowsManualResize)
            {
                compactWidthConstraints.Add(_handlebarSeparator.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor, handleBarToSeparatorMargin));
                compactWidthConstraints.Add(_handlebar.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor, 8));
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor));
            }
            else
            {
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor));
            }

            _compactWidthConstraints = compactWidthConstraints.ToArray();

            _heightConstraint = View.HeightAnchor.ConstraintEqualTo(150);
            _heightConstraint.Active = true;

            PanelTopAnchor = _blurShadowContainerView.TopAnchor;

            ApplyConstraints();
        }

        private void HandleMoveView(UIPanGestureRecognizer recognizer)
        {
            if (!AllowsManualResize)
            {
                return;
            }
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

                if (_heightConstraint.Constant == minHeight && AllowsMinimumHeight)
                {
                    _currentState = BottomSheetState.minimized;
                }
                else if (_heightConstraint.Constant == MaxHeightConstraint)
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
            nfloat baseHeight;
            if (DisplayedContentView.Subviews.FirstOrDefault() is IntrinsicContentSizedStackView stackView)
            {
                baseHeight = stackView.SystemLayoutSizeFittingSize(new CoreGraphics.CGSize(-1, -1)).Height;
            }
            else
            {
                baseHeight = partialHeight;
            }

            if (AllowsManualResize)
            {
                baseHeight += 12.5f; // size of resize UI elements
            }

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact)
            {
                baseHeight += 8f; // 8f is extra margin

                // account for bottom safe area
                baseHeight += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
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
                if (_handlebarSeparator != null)
                {
                    _handlebarSeparator.BackgroundColor = _handlebarColor;
                }
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_compactWidthConstraints);
                if (_handlebarSeparator != null)
                {
                    _handlebarSeparator.BackgroundColor = UIColor.Clear;
                }
            }

            SetStateWithAnimation(_currentState);
        }
    }
}