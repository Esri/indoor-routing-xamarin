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
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
    {
        public MapViewController(MapViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        #region view fields
        // top right buttons
        private UIButton _settingsButton;
        private UIButton _homeButton;
        private UIButton _locationButton;
        private SimpleStackedButtonContainer _accessoryView;

        private FloorsTableView _innerFloorsTableView;
        private UIView _innerFloorsTableViewShadow; // shadow container needs to be hidden for stack layout to work

        private UIStackView _topRightStack;

        private UIVisualEffectView _topBlur;

        private Compass _compass;
        private MapView _mapView;

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
        #endregion view fields

        #region ios lifecycle methods
        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            SubscribeToEvents();

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

            SetAttributionForCurrentState();
        }

        public override void LoadView()
        {
            base.LoadView();

            // Create the view
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor };

            // Create the map view
            _mapView = new MapView { TranslatesAutoresizingMaskIntoConstraints = false };
            ConfigureMapView();
            
            // Create and set up accessory buttons
            _settingsButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _settingsButton.SetImage(UIImage.FromBundle("gear"), UIControlState.Normal);
            _settingsButton.AccessibilityLabel = "Show settings".Localize();
            _settingsButton.AccessibilityHint = "Open settings for this app".Localize();

            _homeButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = !AppSettings.CurrentSettings.IsHomeSet };
            _homeButton.SetImage(UIImage.FromBundle("home"), UIControlState.Normal);
            _homeButton.AccessibilityLabel = "Go home".Localize();
            _homeButton.AccessibilityHint = "Select your home location".Localize();

            _locationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = !AppSettings.CurrentSettings.IsLocationServicesEnabled };
            _locationButton.SetImage(UIImage.FromBundle("gps-on"), UIControlState.Normal);
            _locationButton.AccessibilityLabel = "Go to current location".Localize();
            _locationButton.AccessibilityHint = "Select your device's current location".Localize();

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

            // Bottom sheet has to be done last
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

        #endregion ios lifecycle methods

        #region additional view setup
        private void ConfigureBottomSheet()
        {
            _bottomSheet = new BottomSheetViewController(View);

            AddChildViewController(_bottomSheet);

            _bottomSheet.DidMoveToParentViewController(this);

            _locationCard = new LocationInfoCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _routeResultView = new RouteResultCard(_viewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _routeResultView.RelayoutRequested += RouteCard_LayoutRequested;

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

            ConfigureAttribution();

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
                containerView.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor)
            });

            // set initial height
            _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
        }

        private void ConfigureAttribution()
        {
            _attributionStack = new UIStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Trailing,
                Spacing = ApplicationTheme.Margin
            };

            _attributionImageButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false, TintColor = UIColor.SystemBackgroundColor };
            _attributionImageButton.SetImage(UIImage.FromBundle("information").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate), UIControlState.Normal);

            _esriIcon = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TintColor = UIColor.SystemBackgroundColor,
                AdjustsImageWhenHighlighted = false
            };
            _esriIcon.SetImage(UIImage.FromBundle("esri"), UIControlState.Normal);
            _esriIcon.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;


            _attributionStack.AddArrangedSubview(_esriIcon);
            _attributionStack.AddArrangedSubview(_attributionImageButton);

            // put map attribution directly above map so it is under accessory views
            _shadowedAttribution = _attributionStack.EncapsulateInShadowView();
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

        private void SetAttributionForCurrentState()
        {
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

            _attributionImageButton.Hidden = String.IsNullOrWhiteSpace(_mapView.AttributionText);
        }

        #endregion additional view setup

        #region react to view model changes
        private void CurrentSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            AppSettings settings = (AppSettings)sender;
            switch (e.PropertyName)
            {
                case nameof(AppSettings.UseOnlineBasemap):
                    if (settings.UseOnlineBasemap && _mapView?.Map != null)
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
                        _homeButton.Hidden = !settings.IsHomeSet;
                        _accessoryView.ReloadData();
                    }
                    break;
                case nameof(AppSettings.IsLocationServicesEnabled):
                    if (_locationButton != null)
                    {
                        _locationButton.Hidden = !settings.IsLocationServicesEnabled;
                        _accessoryView.ReloadData();
                    }
                    break;
                case nameof(AppSettings.MapViewMinScale):
                    if (_mapView?.Map != null)
                    {
                        _mapView.Map.MinScale = settings.MapViewMinScale;
                    }
                    break;
                case nameof(AppSettings.MapViewMaxScale):
                    if (_mapView?.Map != null)
                    {
                        _mapView.Map.MaxScale = settings.MapViewMaxScale;
                    }
                    break;
            }
        }

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
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.LocationFound:
                    _locationCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.LocationNotFound:
                    _notFoundCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.PlanningRoute:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.RouteFound:
                    _routeResultView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.RouteNotFound:
                    _notFoundCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.SearchingForDestination:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.SearchingForOrigin:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.SearchingForFeature:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Full);
                    break;
                case UiState.DestinationFound:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.OriginFound:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case UiState.FeatureSearchEntered:
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);
                    break;
            }
        }
        #endregion

        #region event subscription management
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
                _routeResultView.RelayoutRequested += RouteCard_LayoutRequested;
            }
        }

        private void RouteCard_LayoutRequested(object sender, EventArgs e) => _bottomSheet?.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.Partial);

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

            //  the settings button
            _settingsButton.TouchUpInside -= SettingsButton_Clicked;

            // Home button
            _homeButton.TouchUpInside -= Home_TouchUpInside;

            // location button
            _locationButton.TouchUpInside -= CurrentLocationButton_TouchUpInside;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside -= Attribution_Tapped;
            }

            // set up events
            _viewModel.PropertyChanged -= ViewModelPropertyChanged;
            AppSettings.CurrentSettings.PropertyChanged -= CurrentSettings_PropertyChanged;

            if (_routeResultView != null)
            {
                _routeResultView.RelayoutRequested -= RouteCard_LayoutRequested;
            }
        }
        #endregion event subscription management
    }
}
