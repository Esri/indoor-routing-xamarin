using System;
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
            // minimized is used to set a default minimum size; controlled by AllowsMinimumHeight
            minimized,
            // Partial fits intrinsic size of content, assuming content is in stack view
            partial,
            full
        };

        private BottomSheetState _currentState = BottomSheetState.partial;

        private UIPanGestureRecognizer _gesture;
        private UIView _containerView;

        private UIView _handlebar;
        private UIView _handlebarSeparator;

        private UIView _blurShadowContainerView;

        public nfloat DefaultPartialHeight { get; set; } = 160;

        public nfloat MinimumHeight { get; set; } = 80;

        public bool AllowsMinimumHeight { get; set; } = false;

        public bool AllowsManualResize { get; set; } = false;

        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint _heightConstraint;

        public UIView DisplayedContentView { get; } = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };

        // Exposed so that other things (e.g. attribution button) can be anchored
        public NSLayoutYAxisAnchor PanelTopAnchor { get; private set; }

        public BottomSheetViewController(UIView container)
        {
            // container view is needed because for constaints to work, view must be in same hierarchy
            _containerView = container;
            _gesture = new UIPanGestureRecognizer(HandleMoveView);

            var blurView = new UIVisualEffectView(ApplicationTheme.PanelBackgroundMaterial)
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
                _handlebar.Layer.CornerRadius = ApplicationTheme.HandlebarCornerRadius;
                _handlebar.BackgroundColor = ApplicationTheme.SeparatorColor;
                blurView.ContentView.AddSubview(_handlebar);

                _handlebarSeparator = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
                _handlebarSeparator.BackgroundColor = ApplicationTheme.SeparatorColor;
                blurView.ContentView.AddSubview(_handlebarSeparator);

                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    _handlebar.CenterXAnchor.ConstraintEqualTo(blurView.CenterXAnchor),
                    _handlebar.HeightAnchor.ConstraintEqualTo(ApplicationTheme.HandlebarThickness),
                    _handlebar.WidthAnchor.ConstraintEqualTo(ApplicationTheme.HandlebarLength),
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

            var regularWidthConstraints = new List<NSLayoutConstraint>()
            {
                _blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, ApplicationTheme.Margin),
                _blurShadowContainerView.WidthAnchor.ConstraintEqualTo(320),
                _blurShadowContainerView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, ApplicationTheme.Margin),
                _blurShadowContainerView.BottomAnchor.ConstraintGreaterThanOrEqualTo(_blurShadowContainerView.TopAnchor, MinimumHeight - (2 * ApplicationTheme.Margin)),
                _blurShadowContainerView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                
                DisplayedContentView.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor),
            };

            if (AllowsManualResize)
            {
                regularWidthConstraints.Add(_handlebar.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor, -(0.5f * ApplicationTheme.Margin)));
                regularWidthConstraints.Add(DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebarSeparator.TopAnchor, -ApplicationTheme.Margin));
                regularWidthConstraints.Add(_handlebarSeparator.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -(0.5f * ApplicationTheme.Margin)));
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
                _blurShadowContainerView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, ApplicationTheme.Margin),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(_blurShadowContainerView.BottomAnchor)
            };

            if (AllowsManualResize)
            {
                compactWidthConstraints.Add(_handlebarSeparator.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor, (0.5f * ApplicationTheme.Margin)));
                compactWidthConstraints.Add(_handlebar.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor, ApplicationTheme.Margin));
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor));
            }
            else
            {
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(_blurShadowContainerView.TopAnchor));
            }

            _compactWidthConstraints = compactWidthConstraints.ToArray();

            _heightConstraint = View.HeightAnchor.ConstraintEqualTo(DefaultPartialHeight);
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
            if (_heightConstraint.Constant < MinimumHeight)
            {
                _heightConstraint.Constant = MinimumHeight;
            }

            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                if (Math.Abs(recognizer.VelocityInView(View).Y) > 0)
                {
                    AnimateSwitchState(recognizer);
                }

                if (_heightConstraint.Constant == MinimumHeight && AllowsMinimumHeight)
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
                        return _containerView.Frame.Height + ApplicationTheme.Margin - _containerView.SafeAreaInsets.Top - (2 * ApplicationTheme.Margin);
                    case UIUserInterfaceSizeClass.Regular:
                    default:
                        return _containerView.Frame.Height - _containerView.SafeAreaInsets.Top - (2 * ApplicationTheme.Margin) - _containerView.SafeAreaInsets.Bottom - (2 * ApplicationTheme.Margin);
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
                baseHeight = DefaultPartialHeight;
            }

            if (AllowsManualResize)
            {
                baseHeight += 0.5f + (1.5f * ApplicationTheme.Margin); // size of resize UI elements
            }

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact)
            {
                baseHeight += ApplicationTheme.Margin; // margin from bottom safe area

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
                    UIView.Animate(ApplicationTheme.SimpleAnimationDuration, () =>
                    {
                        _heightConstraint.Constant = GetPartialHeight();
                    });
                    break;
                case BottomSheetState.minimized:
                    UIView.Animate(ApplicationTheme.SimpleAnimationDuration, () =>
                    {
                        _heightConstraint.Constant = MinimumHeight;
                    });
                    break;
                case BottomSheetState.full:
                    UIView.Animate(ApplicationTheme.SimpleAnimationDuration, () =>
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

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);
            ApplyConstraints();
        }

        private void ApplyConstraints()
        {
            NSLayoutConstraint.DeactivateConstraints(_regularWidthConstraints);
            NSLayoutConstraint.DeactivateConstraints(_compactWidthConstraints);
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_regularWidthConstraints);
                if (_handlebarSeparator != null)
                {
                    _handlebarSeparator.BackgroundColor = ApplicationTheme.SeparatorColor;
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