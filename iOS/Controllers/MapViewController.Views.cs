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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
    {
        private MapView _mapView;
        // Stack view arranges the accessory buttons, floors tableview, and compass
        private UIStackView _topRightStack;

        // top right buttons
        private UIButton _settingsButton;
        private UIButton _homeButton;
        private UIButton _locationButton;
        private SimpleStackedButtonContainer _accessoryView;

        private FloorsTableView _innerFloorsTableView;
        private UIView _innerFloorsTableViewShadow;

        private Compass _compass;

        // View shown at the top to make sure the system area is legible
        private UIVisualEffectView _topBlur;

        // Displays the bottom sheet in compact width and the side panel in regular width
        private BottomSheetViewController _bottomSheet;

        // Bottom sheet cards
        private LocationSearchCard _locationSearchCard;
        private LocationInfoCard _locationCard;
        private RoutePlanningCard _routeSearchView;
        private RouteResultCard _routeResultView;
        private NotFoundCard _notFoundCard;

        // Attribution image
        private UIButton _esriIcon;
        private UIButton _attributionImageButton;
        private UIStackView _attributionStack;
        private UIView _shadowedAttribution;

        public MapViewController(MapViewModel viewModel) => _viewModel = viewModel;

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            SubscribeToEvents();

            // Before iOS 13, the main view disappears, causing the event subscriptions keeping things up to date to be unsubscribed.
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                if (AppSettings.CurrentSettings.UseOnlineBasemap && _mapView?.Map != null)
                {
                    _mapView.Map.Basemap = Basemap.CreateLightGrayCanvasVector();
                }
                else if (_mapView?.Map != null)
                {
                    _mapView.Map.Basemap = null;
                }
                if (_homeButton != null)
                {
                    _homeButton.Hidden = !AppSettings.CurrentSettings.IsHomeSet;
                }
                _mapView.LocationDisplay.LocationChanged -= MapView_LocationChanged;
                if (_locationButton != null)
                {
                    _locationButton.Hidden = !AppSettings.CurrentSettings.IsLocationServicesEnabled;
                }
                if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
                {
                    _mapView.LocationDisplay.IsEnabled = true;
                    _mapView.LocationDisplay.LocationChanged += MapView_LocationChanged;
                }
                else
                {
                    _mapView.LocationDisplay.IsEnabled = false;
                }
                if (_mapView?.Map != null)
                {
                    _mapView.Map.MinScale = AppSettings.CurrentSettings.MapViewMinScale;
                }
                if (_mapView?.Map != null)
                {
                    _mapView.Map.MaxScale = AppSettings.CurrentSettings.MapViewMaxScale;
                }
                _accessoryView.ReloadData();
            }

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            UnsubscribeFromEvents();

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = false;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            SetAttributionForCurrentState();
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            // Update the attribution views whenever the trait collection changes (e.g. to adapt to size class changes)
            SetAttributionForCurrentState();
        }

        public override void LoadView()
        {
            // Create the view
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor };

            // Create the map view
            _mapView = new MapView { TranslatesAutoresizingMaskIntoConstraints = false };

            // Do all map-related setup, including setting up renderers, layers, and symbols
            ConfigureMapView();

            // Create and set up accessory buttons
            _settingsButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _settingsButton.SetImage(UIImage.FromBundle("gear"), UIControlState.Normal);
            _settingsButton.AccessibilityLabel = "SettingsButtonAccessibilityLabel".Localize();
            _settingsButton.AccessibilityHint = "SettingsButtonAccessibilityHint".Localize();

            _homeButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = !AppSettings.CurrentSettings.IsHomeSet };
            _homeButton.SetImage(UIImage.FromBundle("home"), UIControlState.Normal);
            _homeButton.AccessibilityLabel = "HomeButtonAccessibilityLabel".Localize();
            _homeButton.AccessibilityHint = "HomeButtonAccessibilityHint".Localize();

            _locationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = !AppSettings.CurrentSettings.IsLocationServicesEnabled };
            _locationButton.SetImage(UIImage.FromBundle("gps-on"), UIControlState.Normal);
            _locationButton.AccessibilityLabel = "LocationButtonAccessibilityLabel".Localize();
            _locationButton.AccessibilityHint = "LocationButtonAccessibilityHint".Localize();

            if (_mapView.LocationDisplay?.AutoPanMode == LocationDisplayAutoPanMode.Recenter && _mapView.LocationDisplay?.IsEnabled == true)
            {
                _locationButton.SetImage(UIImage.FromBundle("gps-on-f"), UIControlState.Normal);
            }

            _topRightStack = new IntrinsicContentSizedStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = ApplicationTheme.Margin,
                Distribution = UIStackViewDistribution.EqualSpacing
            };

            _accessoryView = new SimpleStackedButtonContainer(new[] { _homeButton, _settingsButton, _locationButton })
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var accessoryShadowContainer = _accessoryView.EncapsulateInShadowView();

            _innerFloorsTableView = new FloorsTableView(_viewModel) { TranslatesAutoresizingMaskIntoConstraints = false };
            _innerFloorsTableViewShadow = _innerFloorsTableView.EncapsulateInShadowView();

            _compass = new Compass { TranslatesAutoresizingMaskIntoConstraints = false, GeoView = _mapView };

            _topBlur = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial)) { TranslatesAutoresizingMaskIntoConstraints = false };

            View.AddSubviews(_mapView, _topRightStack, _topBlur);

            _topRightStack.AddArrangedSubview(accessoryShadowContainer);
            _topRightStack.AddArrangedSubview(_innerFloorsTableViewShadow);
            _topRightStack.AddArrangedSubview(_compass);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _mapView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                // top-right floating buttons
                _topRightStack.TopAnchor.ConstraintEqualTo(_topBlur.BottomAnchor, ApplicationTheme.Margin),
                _topRightStack.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -ApplicationTheme.Margin),
                // compass sizing
                _compass.WidthAnchor.ConstraintEqualTo(ApplicationTheme.SideWidgetWidth),
                _compass.HeightAnchor.ConstraintEqualTo(_compass.WidthAnchor),
                // right panel accessories
                accessoryShadowContainer.HeightAnchor.ConstraintEqualTo(_accessoryView.HeightAnchor),
                accessoryShadowContainer.WidthAnchor.ConstraintEqualTo(ApplicationTheme.SideWidgetWidth),
                // floors view
                _innerFloorsTableViewShadow.WidthAnchor.ConstraintEqualTo(accessoryShadowContainer.WidthAnchor),
                _innerFloorsTableViewShadow.HeightAnchor.ConstraintLessThanOrEqualTo(ApplicationTheme.FloorWidthMaxHeight),
                // Top blur (to make handlebar and system area easy to see)
                _topBlur.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _topBlur.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _topBlur.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _topBlur.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor)
            });

            ConfigureBottomSheet();
        }

        /// <summary>
        /// Overrides default behavior when view has loaded. 
        /// </summary>
        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var genericError = "MapLoadError".Localize();

                InvokeOnMainThread(() =>
                {
                    var detailsController = UIAlertController.Create("ErrorDetailAlertTitle".Localize(), ex.Message, UIAlertControllerStyle.Alert);
                    detailsController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".Localize(), UIAlertActionStyle.Default, null));

                    var alertController = UIAlertController.Create("ErrorDetailAlertTitle".Localize(), genericError, UIAlertControllerStyle.Alert);
                    alertController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".Localize(), UIAlertActionStyle.Default, null));
                    alertController.AddAction(
                        UIAlertAction.Create(
                            "ErrorAlertDetailsButtonText".Localize(),
                            UIAlertActionStyle.Default,
                            (obj) => { PresentViewController(detailsController, true, null); }));
                    PresentViewController(alertController, true, null);
                });
            }
        }

        /// <summary>
        /// Sets up the bottom sheet and all the contained views.
        /// Must be called after the rest of the view is set up
        /// </summary>
        private void ConfigureBottomSheet()
        {
            _bottomSheet = new BottomSheetViewController();

            AddChildViewController(_bottomSheet);

            View.AddSubview(_bottomSheet.View);

            _bottomSheet.DidMoveToParentViewController(this);

            _locationCard = new LocationInfoCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _locationCard.RelayoutRequested += Card_RelayoutRequested;

            _routeResultView = new RouteResultCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _routeResultView.RelayoutRequested += Card_RelayoutRequested;

            _locationSearchCard = new LocationSearchCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = false,
                BackgroundColor = UIColor.Clear
            };

            _routeSearchView = new RoutePlanningCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };

            _notFoundCard = new NotFoundCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };

            UIStackView containerView = new IntrinsicContentSizedStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical
            };

            containerView.AddArrangedSubview(_locationSearchCard);
            containerView.AddArrangedSubview(_notFoundCard);
            containerView.AddArrangedSubview(_locationCard);
            containerView.AddArrangedSubview(_routeSearchView);
            containerView.AddArrangedSubview(_routeResultView);

            _bottomSheet.DisplayedContentView.AddSubview(containerView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                containerView.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                containerView.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor),
                containerView.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor),
                containerView.BottomAnchor.ConstraintLessThanOrEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -ApplicationTheme.Margin)
            });

            // set initial height
            _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
        }

        /// <summary>
        /// Sets up attribution UI elements
        /// </summary>
        private void ConfigureAttribution()
        {
            if (_bottomSheet?.PanelTopAnchor == null)
            {
                return;
            }

            // Stack view arranges the esri icon and the info button
            _attributionStack = new UIStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Trailing,
                Spacing = ApplicationTheme.Margin
            };

            _attributionImageButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, TintColor = UIColor.Black };
            _attributionImageButton.SetImage(UIImage.FromBundle("information").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate), UIControlState.Normal);

            _esriIcon = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TintColor = UIColor.Black,
                AdjustsImageWhenHighlighted = false
            };
            _esriIcon.SetImage(UIImage.FromBundle("esri"), UIControlState.Normal);
            _esriIcon.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;


            _attributionStack.AddArrangedSubview(_esriIcon);
            _attributionStack.AddArrangedSubview(_attributionImageButton);

            // Show attribution elements with a shadow for enhanced visibility
            _shadowedAttribution = _attributionStack.EncapsulateInShadowView();

            // put map attribution directly above map so it is under accessory views
            View.InsertSubviewAbove(_shadowedAttribution, _mapView);

            _attributionImageButton.TouchUpInside += Attribution_Tapped;

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _shadowedAttribution.BottomAnchor.ConstraintEqualTo(_bottomSheet.PanelTopAnchor, -ApplicationTheme.Margin),
                _shadowedAttribution.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -ApplicationTheme.Margin),
                _esriIcon.HeightAnchor.ConstraintEqualTo(22), // arbitrary values based on height of icon
                _esriIcon.WidthAnchor.ConstraintEqualTo(63)
            });

            SetAttributionForCurrentState();
        }

        /// <summary>
        /// Hide or show attribution elements based on current map & UI state
        /// </summary>
        private void SetAttributionForCurrentState()
        {
            if (_attributionStack == null)
            {
                // Bottom sheet and attribution setup have to happen after initial view configuration
                ConfigureAttribution();
            }
            if (_shadowedAttribution == null)
            {
                return;
            }

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                _shadowedAttribution.Hidden = true;
                _mapView.IsAttributionTextVisible = true;
            }
            else
            {
                _shadowedAttribution.Hidden = false;
                _mapView.IsAttributionTextVisible = false;
            }

            // The esri icon is always shown, but the info button is only visible if there is attribution text
            _attributionImageButton.Hidden = string.IsNullOrWhiteSpace(_mapView.AttributionText);
        }

        /// <summary>
        /// Update the UI for any settings changes
        /// </summary>
        private void CurrentSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.CurrentSettings.UseOnlineBasemap):
                    if (AppSettings.CurrentSettings.UseOnlineBasemap && _mapView?.Map != null)
                    {
                        _mapView.Map.Basemap = Basemap.CreateLightGrayCanvasVector();
                    }
                    else if (_mapView?.Map != null)
                    {
                        _mapView.Map.Basemap = null;
                    }
                    break;
                case nameof(AppSettings.IsHomeSet):
                    if (_homeButton != null)
                    {
                        _homeButton.Hidden = !AppSettings.CurrentSettings.IsHomeSet;
                        // The accessory view will resize itself for any hidden buttons when ReloadData is called
                        _accessoryView.ReloadData();
                    }
                    break;
                case nameof(AppSettings.IsLocationServicesEnabled):
                    _mapView.LocationDisplay.LocationChanged -= MapView_LocationChanged;
                    if (_locationButton != null)
                    {
                        _locationButton.Hidden = !AppSettings.CurrentSettings.IsLocationServicesEnabled;
                        _accessoryView.ReloadData();
                    }
                    if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
                    {
                        _mapView.LocationDisplay.IsEnabled = true;
                        _mapView.LocationDisplay.LocationChanged += MapView_LocationChanged;
                    }
                    else
                    {
                        _mapView.LocationDisplay.IsEnabled = false;
                    }
                    break;
                case nameof(AppSettings.MapViewMinScale):
                    if (_mapView?.Map != null)
                    {
                        _mapView.Map.MinScale = AppSettings.CurrentSettings.MapViewMinScale;
                    }
                    break;
                case nameof(AppSettings.MapViewMaxScale):
                    if (_mapView?.Map != null)
                    {
                        _mapView.Map.MaxScale = AppSettings.CurrentSettings.MapViewMaxScale;
                    }
                    break;
            }
        }

        /// <summary>
        /// Hide and show UI elements as needed for the current state
        /// </summary>
        private void UpdateUiForNewState()
        {
            _locationSearchCard.Hidden = true;
            _locationCard.Hidden = true;
            _routeSearchView.Hidden = true;
            _routeResultView.Hidden = true;
            _notFoundCard.Hidden = true;

            switch (_viewModel.CurrentState)
            {
                case UiState.ReadyWaiting:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.LocationFound:
                    _locationCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.LocationNotFound:
                    _notFoundCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.PlanningRoute:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.RouteFound:
                    _routeResultView.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.RouteNotFound:
                    _notFoundCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.SearchingForDestination:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.SearchingForOrigin:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.SearchingForFeature:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.FeatureSearchEntered:
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
            }
        }

        /// <summary>
        /// Subscribe to all events
        /// </summary>
        private void SubscribeToEvents()
        {
            // set up events
            _viewModel.PropertyChanged += ViewModelPropertyChanged;

            // Handle the user moving the map 
            _mapView.NavigationCompleted += MapView_NavigationCompleted;

            // Handle the user tapping on the map
            _mapView.GeoViewTapped += MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            _mapView.GeoViewDoubleTapped += MapView_GeoViewDoubleTapped;

            _mapView.LocationDisplay.LocationChanged += MapView_LocationChanged;

            _mapView.LocationDisplay.AutoPanModeChanged += LocationDisplay_AutoPanModeChanged;

            //  the settings button
            _settingsButton.TouchUpInside += SettingsButton_Clicked;

            // Home button
            _homeButton.TouchUpInside += Home_TouchUpInside;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside += Attribution_Tapped;
            }

            // location button
            _locationButton.TouchUpInside += CurrentLocationButton_TouchUpInside;

            // handle settings changes
            AppSettings.CurrentSettings.PropertyChanged += CurrentSettings_PropertyChanged;

            if (_routeResultView != null)
            {
                _routeResultView.RelayoutRequested += Card_RelayoutRequested;
            }

            if (_locationCard != null)
            {
                _locationCard.RelayoutRequested += Card_RelayoutRequested;
            }
        }

        private void LocationDisplay_AutoPanModeChanged(object sender, LocationDisplayAutoPanMode e)
        {
            switch (e)
            {
                case LocationDisplayAutoPanMode.Recenter:
                    InvokeOnMainThread(() => _locationButton.SetImage(UIImage.FromBundle("gps-on-f"), UIControlState.Normal));
                    break;
                case LocationDisplayAutoPanMode.Off:
                    InvokeOnMainThread(() => _locationButton.SetImage(UIImage.FromBundle("gps-on"), UIControlState.Normal));
                    break;
            }
        }

        /// <summary>
        /// Updates the size of the bottom sheet when requested by one of the contained cards.
        /// </summary>
        private void Card_RelayoutRequested(object sender, EventArgs e) => _bottomSheet?.SetState(BottomSheetViewController.BottomSheetState.Partial);

        /// <summary>
        /// Unsubscribe from all events to prevent memory leaks
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // set up events
            _viewModel.PropertyChanged -= ViewModelPropertyChanged;

            // Handle the user moving the map 
            _mapView.NavigationCompleted -= MapView_NavigationCompleted;

            // Handle the user tapping on the map
            _mapView.GeoViewTapped -= MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            _mapView.GeoViewDoubleTapped -= MapView_GeoViewDoubleTapped;

            _mapView.LocationDisplay.LocationChanged -= MapView_LocationChanged;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside -= Attribution_Tapped;
            }

            _mapView.LocationDisplay.AutoPanModeChanged -= LocationDisplay_AutoPanModeChanged;

            //  the settings button
            _settingsButton.TouchUpInside -= SettingsButton_Clicked;

            // Home button
            _homeButton.TouchUpInside -= Home_TouchUpInside;

            // location button
            _locationButton.TouchUpInside -= CurrentLocationButton_TouchUpInside;

            // set up events
            _viewModel.PropertyChanged -= ViewModelPropertyChanged;
            AppSettings.CurrentSettings.PropertyChanged -= CurrentSettings_PropertyChanged;

            if (_routeResultView != null)
            {
                _routeResultView.RelayoutRequested -= Card_RelayoutRequested;
            }

            if (_locationCard != null)
            {
                _locationCard.RelayoutRequested -= Card_RelayoutRequested;
            }
        }
    }
}
