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
using CoreGraphics;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Manages display and layout of a 'bottom sheet' when in compact width, and a side panel in regular width.
    /// </summary>
    public sealed class BottomSheetViewController : UIViewController
    {
        // Tracks the current state of the bottom sheet
        private BottomSheetState _currentState = BottomSheetState.Partial;

        // Handlebar is the pill-shaped view that indicates the view is draggable.
        private UIView _handlebar;
        private UIView _handlebarSeparator;

        // Constraints are stored so that they can be disabled, enabled, and modified as needed.
        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint _heightConstraint;

        public override void LoadView()
        {
            // Special view ignores touches, except for on subviews
            View = new TouchTransparentView { TranslatesAutoresizingMaskIntoConstraints = false };

            var gesture = new UIPanGestureRecognizer(HandleMoveView);

            var blurView = new UIVisualEffectView(ApplicationTheme.PanelBackgroundMaterial)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ClipsToBounds = true
            };

            // Defined in Helpers/ViewExtensions
            var blurShadowContainerView = blurView.EncapsulateInShadowView();
            View.AddSubview(blurShadowContainerView);

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
                blurView.TopAnchor.ConstraintGreaterThanOrEqualTo(View.SafeAreaLayoutGuide.TopAnchor)
            });

            var regularWidthConstraints = new List<NSLayoutConstraint>()
            {
                blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, ApplicationTheme.Margin),
                blurShadowContainerView.WidthAnchor.ConstraintEqualTo(320),
                blurShadowContainerView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, ApplicationTheme.Margin),
                blurShadowContainerView.BottomAnchor.ConstraintGreaterThanOrEqualTo(blurShadowContainerView.TopAnchor, MinimumHeight - (2 * ApplicationTheme.Margin)),
                blurShadowContainerView.BottomAnchor.ConstraintLessThanOrEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),

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
                blurShadowContainerView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                blurShadowContainerView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                blurShadowContainerView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, ApplicationTheme.Margin),
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

            _heightConstraint = blurShadowContainerView.HeightAnchor.ConstraintEqualTo(DefaultPartialHeight);
            _heightConstraint.Active = true;

            PanelTopAnchor = blurShadowContainerView.TopAnchor;

            UpdateInterfaceForCurrentTraits();
        }

        /// <summary>
        /// Enumeration of possible layout states
        /// </summary>
        public enum BottomSheetState
        {
            // Minimized is used to set a default minimum size; controlled by AllowsMinimizedState
            Minimized,
            // Fits intrinsic size of content, assuming content is in stack view
            Partial,
            // Fills available vertical space
            Full
        }

        /// <summary>
        /// Container for the view that will be displayed in the bottom sheet/side panel
        /// </summary>
        public UIView DisplayedContentView { get; } = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };

        /// <summary>
        /// Anchor to use for constraining views (e.g. attribution) to the top of this panel when in compact width (bottom sheet) mode.
        /// </summary>
        public NSLayoutYAxisAnchor PanelTopAnchor { get; private set; }

        /// <summary>
        /// Defines the height to use when in the partial state and the height of the content can't be determined.
        /// </summary>
        public nfloat DefaultPartialHeight { get; set; } = 160;

        /// <summary>
        /// Defines the size of the content view when the view state is minimized.
        /// Generally only used if <see cref="AllowsMinimizedState"/> or <see cref="AllowsManualResize"/> is <value>true</value>.
        /// </summary>
        public nfloat MinimumHeight { get; set; } = 80;

        /// <summary>
        /// Determines if the view can be set to the Minimized state. If <value>false</value>, the partial state is used in place of minimized.
        /// </summary>
        public bool AllowsMinimizedState { get; set; } = false;

        /// <summary>
        /// If <value>true</value>, the user can pan to adjust the size of the view. When <value>true</value>,
        /// a handlebar is shown to indicate that the view is adjustable.
        /// </summary>
        public bool AllowsManualResize { get; set; } = false;

        /// <summary>
        /// Gets the maximum height of the sheet based on the current UI trait collection
        /// </summary>
        private nfloat MaxHeightConstraint
        {
            get
            {
                switch (TraitCollection.HorizontalSizeClass)
                {
                    case UIUserInterfaceSizeClass.Compact:
                        return View.Frame.Height + ApplicationTheme.Margin - View.SafeAreaInsets.Top - (2 * ApplicationTheme.Margin);
                    case UIUserInterfaceSizeClass.Regular:
                    default:
                        return View.Frame.Height - View.SafeAreaInsets.Top - (2 * ApplicationTheme.Margin) - View.SafeAreaInsets.Bottom - (2 * ApplicationTheme.Margin);
                }
            }
        }


        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            UpdateInterfaceForCurrentTraits();
        }

        /// <summary>
        /// Sets the bottom sheet state and updates the height of the sheet as needed.
        /// </summary>
        /// <param name="state"></param>
        public void SetState(BottomSheetState state)
        {
            _currentState = state;
            if (_heightConstraint == null)
            {
                return;
            }
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

        /// <summary>
        /// Updates layout constraints and other properties based on the current UI size class.
        /// </summary>
        private void UpdateInterfaceForCurrentTraits()
        {
            // First deactivate existing constraints
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

            SetState(_currentState);
        }

        /// <summary>
        /// Handles flick gesture
        /// </summary>
        /// <param name="recognizer">Gesture recognizer that is handling user interaction </param>
        private void HandleFlick(UIPanGestureRecognizer recognizer)
        {
            switch (_currentState)
            {
                case BottomSheetState.Minimized:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetState(BottomSheetState.Partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetState(BottomSheetState.Partial);
                    }
                    break;
                case BottomSheetState.Partial:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetState(BottomSheetState.Full);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetState(BottomSheetState.Minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetState(BottomSheetState.Minimized);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetState(BottomSheetState.Full);
                    }
                    break;
                case BottomSheetState.Full:
                    if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact && recognizer.VelocityInView(View).Y > 0)
                    {
                        SetState(BottomSheetState.Partial);
                    }
                    else if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular && recognizer.VelocityInView(View).Y < 0)
                    {
                        SetState(BottomSheetState.Partial);
                    }
                    break;
            }
        }

        /// <summary>
        /// Adjusts the size of the view when the user pans (if <see cref="AllowsManualResize"/> is <value>true</value>).
        /// </summary>
        /// <param name="recognizer">Gesture recognizer that is handling user interaction.</param>
        private void HandleMoveView(UIPanGestureRecognizer recognizer)
        {
            // Do nothing if user resize isn't enabled
            if (!AllowsManualResize)
            {
                return;
            }

            // Get the distance moved
            var translation = recognizer.TranslationInView(View);

            // In compact width scrolling up (negative translation) increases height as the card moves up
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

            // Prevent making the view too large
            if (_heightConstraint.Constant > MaxHeightConstraint)
            {
                _heightConstraint.Constant = MaxHeightConstraint;
            }

            // Prevent making the view too small
            if (_heightConstraint.Constant < MinimumHeight)
            {
                _heightConstraint.Constant = MinimumHeight;
            }

            // Enables 'flick' gesture to switch between states
            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                if (Math.Abs(recognizer.VelocityInView(View).Y) > 0)
                {
                    HandleFlick(recognizer);
                }

                if (_heightConstraint.Constant == MinimumHeight && AllowsMinimizedState)
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

        /// <summary>
        /// Returns the height the view should be when in the partial state.
        /// This accounts for all settings, the height of content, and the height of margins and resize indicators (handlebars)
        /// </summary>
        private nfloat GetPartialHeight()
        {
            nfloat baseHeight;

            // Start with the height of the contained content if available, or use the default height.
            if (DisplayedContentView.Subviews.FirstOrDefault() is IntrinsicContentSizedStackView stackView)
            {
                baseHeight = stackView.SystemLayoutSizeFittingSize(new CoreGraphics.CGSize(-1, -1)).Height;
            }
            else
            {
                baseHeight = DefaultPartialHeight;
            }

            // Add the height of resize indicators
            if (AllowsManualResize)
            {
                baseHeight += 0.5f + (1.5f * ApplicationTheme.Margin); // size of resize UI elements
            }

            // Add additional padding at the bottom if this is a phone.
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact)
            {
                // Margin from bottom safe area
                baseHeight += ApplicationTheme.Margin;

                // Space taken by bottom area
                baseHeight += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
            }

            return baseHeight;
        }
    }

    /// <summary>
    /// View with special behavior that ignores touches on transparent children and itself.
    /// </summary>
    class TouchTransparentView : UIView
    {
        public UIView TargetView { get; set; }

        public override bool PointInside(CGPoint point, UIEvent pointEvent)
        {
            foreach (var subview in this.Subviews)
            {
                if (!subview.Hidden && subview.Alpha > 0 && subview.UserInteractionEnabled && subview.Frame.Contains(point))
                {
                    return true;
                }
            }
            return false;
        }
    }
}