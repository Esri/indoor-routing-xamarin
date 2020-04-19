// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public sealed class BottomSheetViewController : UIViewController
    {
        public enum BottomSheetState
        {
            // minimized is used to set a default minimum size; controlled by AllowsMinimumHeight
            Minimized,
            // Partial fits intrinsic size of content, assuming content is in stack view
            Partial,
            Full
        };

        private BottomSheetState _currentState = BottomSheetState.Partial;

        private readonly UIView _containerView;

        private readonly UIView _handlebar;
        private readonly UIView _handlebarSeparator;

        public nfloat DefaultPartialHeight { get; set; } = 160;

        public nfloat MinimumHeight { get; set; } = 80;

        public bool AllowsMinimumHeight { get; set; } = false;

        public bool AllowsManualResize { get; set; } = false;

        private readonly NSLayoutConstraint[] _regularWidthConstraints;
        private readonly NSLayoutConstraint[] _compactWidthConstraints;
        private readonly NSLayoutConstraint _heightConstraint;

        public UIView DisplayedContentView { get; } = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };

        // Exposed so that other things (e.g. attribution button) can be anchored
        public NSLayoutYAxisAnchor PanelTopAnchor { get; }

        public BottomSheetViewController(UIView container)
        {
            // container view is needed because for constraints to work, view must be in same hierarchy
            _containerView = container;
            var gesture = new UIPanGestureRecognizer(HandleMoveView);

            var blurView = new UIVisualEffectView(ApplicationTheme.PanelBackgroundMaterial)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ClipsToBounds = true
            };

            // Defined in Helpers/ViewExtensions
            var blurShadowContainerView = blurView.EncapsulateInShadowView();

            View = blurShadowContainerView;
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

                _handlebarSeparator = new UIView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    BackgroundColor = ApplicationTheme.SeparatorColor
                };
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

                blurView.AddGestureRecognizer(gesture);
            }

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                DisplayedContentView.LeadingAnchor.ConstraintEqualTo(blurView.LeadingAnchor),
                DisplayedContentView.TrailingAnchor.ConstraintEqualTo(blurView.TrailingAnchor),
                blurView.TopAnchor.ConstraintGreaterThanOrEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor)
            });

            var regularWidthConstraints = new List<NSLayoutConstraint>()
            {
                blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.LeadingAnchor, ApplicationTheme.Margin),
                blurShadowContainerView.WidthAnchor.ConstraintEqualTo(320),
                blurShadowContainerView.TopAnchor.ConstraintEqualTo(_containerView.SafeAreaLayoutGuide.TopAnchor, ApplicationTheme.Margin),
                blurShadowContainerView.BottomAnchor.ConstraintGreaterThanOrEqualTo(blurShadowContainerView.TopAnchor, MinimumHeight - (2 * ApplicationTheme.Margin)),
                blurShadowContainerView.BottomAnchor.ConstraintLessThanOrEqualTo(_containerView.SafeAreaLayoutGuide.BottomAnchor),
                
                DisplayedContentView.TopAnchor.ConstraintEqualTo(blurShadowContainerView.TopAnchor),
            };

            if (AllowsManualResize)
            {
                regularWidthConstraints.Add(_handlebar.BottomAnchor.ConstraintEqualTo(blurShadowContainerView.BottomAnchor, -(0.5f * ApplicationTheme.Margin)));
                regularWidthConstraints.Add(DisplayedContentView.BottomAnchor.ConstraintEqualTo(_handlebarSeparator.TopAnchor, -ApplicationTheme.Margin));
                regularWidthConstraints.Add(_handlebarSeparator.BottomAnchor.ConstraintEqualTo(_handlebar.TopAnchor, -(0.5f * ApplicationTheme.Margin)));
            }
            else
            {
                regularWidthConstraints.Add(DisplayedContentView.BottomAnchor.ConstraintEqualTo(blurShadowContainerView.BottomAnchor));
            }

            _regularWidthConstraints = regularWidthConstraints.ToArray();

            var compactWidthConstraints = new List<NSLayoutConstraint>
            {
                blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(_containerView.LeadingAnchor),
                blurShadowContainerView.TrailingAnchor.ConstraintEqualTo(_containerView.TrailingAnchor),
                blurShadowContainerView.BottomAnchor.ConstraintEqualTo(_containerView.BottomAnchor, ApplicationTheme.Margin),
                DisplayedContentView.BottomAnchor.ConstraintEqualTo(blurShadowContainerView.BottomAnchor)
            };

            if (AllowsManualResize)
            {
                compactWidthConstraints.Add(_handlebarSeparator.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor, (0.5f * ApplicationTheme.Margin)));
                compactWidthConstraints.Add(_handlebar.TopAnchor.ConstraintEqualTo(blurShadowContainerView.TopAnchor, ApplicationTheme.Margin));
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(_handlebar.BottomAnchor));
            }
            else
            {
                compactWidthConstraints.Add(DisplayedContentView.TopAnchor.ConstraintEqualTo(blurShadowContainerView.TopAnchor));
            }

            _compactWidthConstraints = compactWidthConstraints.ToArray();

            _heightConstraint = View.HeightAnchor.ConstraintEqualTo(DefaultPartialHeight);
            _heightConstraint.Active = true;

            PanelTopAnchor = blurShadowContainerView.TopAnchor;

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
                    _currentState = BottomSheetState.Minimized;
                }
                else if (_heightConstraint.Constant == MaxHeightConstraint)
                {
                    _currentState = BottomSheetState.Full;
                }
                else
                {
                    _currentState = BottomSheetState.Partial;
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
                case BottomSheetState.Partial:
                   _heightConstraint.Constant = GetPartialHeight();
                    break;
                case BottomSheetState.Minimized:
                    _heightConstraint.Constant = MinimumHeight;
                    break;
                case BottomSheetState.Full:
                    _heightConstraint.Constant = MaxHeightConstraint;
                    break;
            }
        }

        private void AnimateSwitchState(UIPanGestureRecognizer recognizer)
        {
            switch (_currentState)
            {
                case BottomSheetState.Minimized:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Partial);
                    }
                    break;
                case BottomSheetState.Partial:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Full);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Full);
                    }
                    break;
                case BottomSheetState.Full:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetStateWithAnimation(BottomSheetState.Partial);
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