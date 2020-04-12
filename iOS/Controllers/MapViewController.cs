// <copyright file="MapViewController.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Location;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
    using Esri.ArcGISRuntime.Symbology;
    using Esri.ArcGISRuntime.Toolkit.UI.Controls;
    using Esri.ArcGISRuntime.UI;
    using Esri.ArcGISRuntime.UI.Controls;
    using Foundation;
    using UIKit;
    using static Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.MapViewModel;

    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
    {
        /// <summary>
        /// Flag used to determine if the view was single or double tapped
        /// </summary>
        private bool _isViewDoubleTapped;

        // Track previously-used search bar to know where to send selected search suggestions
        private UISearchBar _lastSelectedSearchBar;

        /// <summary>
        /// Gets or sets the map view model containing the common logic for dealing with the map
        /// </summary>
        private MapViewModel ViewModel { get; set; }

        private GraphicsOverlay _homeOverlay;
        private GraphicsOverlay _identifiedFeatureOverlay;
        private GraphicsOverlay _routeOverlay;

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            // set up events
            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;
            this.ViewModel.CurrentVisibleFloors.CollectionChanged += CurrentVisibleFloors_CollectionChanged;

            // Handle the user moving the map 
            this._mapView.NavigationCompleted += _mapView_NavigationCompleted;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped += this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped += this.MapView_GeoViewDoubleTapped;

            this._mapView.LocationDisplay.LocationChanged += this.MapView_LocationChanged;

            //  the settings button
            _settingsButton.TouchUpInside += _settingsButton_TouchUpInside;

            // Home button
            _homeButton.TouchUpInside += Home_TouchUpInside;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside += Attribution_Tapped;
            }

            // location button
            _locationButton.TouchUpInside += CurrentLocationButton_TouchUpInside;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;

            // Show home button if user has home location enabled (not set as default value)
            if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                _homeButton.Enabled = true;
            }

            // Show Current Location button if location services is enabled
            _locationButton.Enabled = AppSettings.CurrentSettings.IsLocationServicesEnabled;

            if (AppSettings.CurrentSettings.IsLocationServicesEnabled == true)
            {
                this._mapView.LocationDisplay.IsEnabled = true;
                this._mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                this._mapView.LocationDisplay.InitialZoomScale = 150;

                // TODO: Set floor when available in the API (Update 2?)
            }
            else
            {
                this._mapView.LocationDisplay.IsEnabled = false;
            }
        }

        private void _mapView_NavigationCompleted(object sender, EventArgs e) => ViewModel.CurrentViewpoint = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

        private void CurrentVisibleFloors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel.CurrentVisibleFloors.Any())
            {
                var tableSource = new FloorsTableSource(ViewModel.CurrentVisibleFloors); // TODO make this persistent and update data instead
                _innerFloorsTableView.Source = tableSource;
                _innerFloorsTableView.ReloadData();
                _innerFloorsTableViewShadow.Hidden = false;
                tableSource.TableRowSelected += this.FloorsTableSource_TableRowSelected;

                if (string.IsNullOrEmpty(ViewModel.SelectedFloorLevel) || !ViewModel.CurrentVisibleFloors.Contains(this.ViewModel.SelectedFloorLevel))
                {
                    ViewModel.SelectedFloorLevel = MapViewModel.DefaultFloorLevel;
                }

                var selectedFloorNSIndex = GetTableViewRowIndex(ViewModel.SelectedFloorLevel, ViewModel.CurrentVisibleFloors.ToArray(), 0);
                _innerFloorsTableView.SelectRow(selectedFloorNSIndex, false, UITableViewScrollPosition.None);

                // Turn layers on. If there is no floor selected, first floor will be displayed by default
                this.ViewModel.SetFloorVisibility(true);
            }
            else
            {
                _innerFloorsTableViewShadow.Hidden = true;
            }
        }

        private async void UpdateUIForNewState()
        {
            _locationSearchCard.Hidden = true;
            _locationCard.Hidden = true;
            _routeSearchView.Hidden = true;
            _routeResultView.Hidden = true;
            _locationNotFoundCard.Hidden = true;

            switch (ViewModel.CurrentState)
            {
                case UIState.ReadyWaiting:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.LocationFound:
                    _locationCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.LocationNotFound:
                    _locationNotFoundCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.PlanningRoute:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.RouteFound:
                    _routeResultView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.RouteNotFound:
                    // TODO 
                    throw new NotImplementedException();
                case UIState.SearchingForDestination:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.full);
                    break;
                case UIState.SearchingForOrigin:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.full);
                    break;
                case UIState.SearchingForFeature:
                    _locationSearchCard.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.full);
                    break;
                case UIState.DestinationFound:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.OriginFound:
                    _routeSearchView.Hidden = false;
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case UIState.FeatureSearchEntered:
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
            }
        }

        private void _locationBar_CancelButtonClicked(object sender, EventArgs e)
        {
            (sender as UISearchBar).Text = string.Empty;
            (sender as UISearchBar).ShowsCancelButton = false;
            (sender as UISearchBar).ResignFirstResponder();
        }

        private async void _settingsButton_TouchUpInside(object sender, EventArgs e)
        {
            DismissableNavigationController navController = new DismissableNavigationController(new SettingsController(ViewModel));
            await PresentViewControllerAsync(navController, true);

            navController.DidDismiss += (o, x) =>
            {
                _homeButton.Enabled = !String.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation);
                _locationButton.Enabled = AppSettings.CurrentSettings.IsLocationServicesEnabled;
                _accessoryView.ReloadData();
            };
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // set up events
            this.ViewModel.PropertyChanged -= this.ViewModelPropertyChanged;

            // Handle the user moving the map 
            this._mapView.NavigationCompleted -= _mapView_NavigationCompleted;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped -= this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped -= this.MapView_GeoViewDoubleTapped;

            this._mapView.LocationDisplay.LocationChanged -= this.MapView_LocationChanged;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside -= Attribution_Tapped;
            }

            //  the settings button
            _settingsButton.TouchUpInside -= _settingsButton_TouchUpInside;

            // Home button
            _homeButton.TouchUpInside -= Home_TouchUpInside;

            // location button
            _locationButton.TouchUpInside -= CurrentLocationButton_TouchUpInside;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = false;
        }

        /// <summary>
        /// Overrides default behavior when view has loaded. 
        /// </summary>
        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            try
            {
                await this.ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var genericError = "MapLoadError".AsLocalized();

                this.InvokeOnMainThread(() =>
                {
                    var detailsController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), ex.Message, UIAlertControllerStyle.Alert);
                    detailsController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, null));

                    var alertController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), genericError, UIAlertControllerStyle.Alert);
                    alertController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, null));
                    alertController.AddAction(
                        UIAlertAction.Create(
                            "ErrorAlertDetailsButtonText".AsLocalized(),
                            UIAlertActionStyle.Default,
                            (obj) => { this.PresentViewController(detailsController, true, null); }));
                    this.PresentViewController(alertController, true, null);
                });
            }
        }

        private void ShowErrorAndContinuteWithAction(string message, Action action)
        {
            var alertController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, x => action()));
            this.PresentViewController(alertController, true, null);
        }


        /// <summary>
        /// Fires when a new route is generated
        /// </summary>
        /// <returns>The new route</returns>
        private async Task OnRouteChangedAsync()
        {
            var newRoute = ViewModel.CurrentRoute.Routes.FirstOrDefault();
            if (newRoute == null)
            {
                _routeOverlay.Graphics.Clear();
            }
            else
            {
                // create a picture marker symbol for start pin
                var uiImageStartPin = UIImage.FromBundle("StartCircle");
                var startPin = await uiImageStartPin.ToRuntimeImageAsync();
                var startMarker = new PictureMarkerSymbol(startPin);

                // create a picture marker symbol for end pin
                var uiImageEndPin = UIImage.FromBundle("EndCircle");
                var endPin = await uiImageEndPin.ToRuntimeImageAsync();
                var endMarker = new PictureMarkerSymbol(endPin);

                // Create point graphics
                var startGraphic = new Graphic(newRoute.RouteGeometry.Parts.First().Points.First(), startMarker);
                var endGraphic = new Graphic(newRoute.RouteGeometry.Parts.Last().Points.Last(), endMarker);

                // create a graphic to represent the routee
                var routeSymbol = new SimpleLineSymbol();
                routeSymbol.Width = 5;
                routeSymbol.Style = SimpleLineSymbolStyle.Solid;
                routeSymbol.Color = System.Drawing.Color.FromArgb(127, 18, 121, 193);

                var routeGraphic = new Graphic(newRoute.RouteGeometry, routeSymbol);

                // Add graphics to overlay
                _routeOverlay.Graphics.Clear();
                _routeOverlay.Graphics.Add(routeGraphic);
                _routeOverlay.Graphics.Add(startGraphic);
                _routeOverlay.Graphics.Add(endGraphic);

                // Hide the pins graphics overlay
                _homeOverlay.IsVisible = false;
                _identifiedFeatureOverlay.IsVisible = false;

                ViewModel.CurrentViewpoint = new Viewpoint(newRoute.RouteGeometry); // TODO - create margin around route

                try
                {
                    await this._mapView.SetViewpointGeometryAsync(newRoute.RouteGeometry, 30);
                }
                catch
                {
                    // If panning to the new route fails, just move on
                }
            }
        }

        /// <summary>
        /// Fires when properties change in the MapViewModel
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.Map):
                    if (this.ViewModel.Map != null)
                    {
                        // Add the map to the MapView to be displayedd
                        this._mapView.Map = this.ViewModel.Map;

                        // Update attribution visibility in case it changed.
                        this.InvokeOnMainThread(SetAttributionForCurrentState);
                    }

                    break;
                case nameof(ViewModel.CurrentViewpoint):
                    if (this.ViewModel.CurrentViewpoint != null)
                    {
                        await this._mapView.SetViewpointAsync(this.ViewModel.CurrentViewpoint);
                    }

                    break;
                case nameof(ViewModel.HomeLocation):
                    // create a picture marker symbol
                    var uiImagePin = UIImage.FromBundle("HomePin");
                    var mapPin = await uiImagePin.ToRuntimeImageAsync();
                    var roomMarker = new PictureMarkerSymbol(mapPin);
                    roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                    // Create graphic
                    var mapPinGraphic = new Graphic(ViewModel.HomeLocation, roomMarker);

                    // Add pin to map
                    var graphicsOverlay = this._mapView.GraphicsOverlays["PinsGraphicsOverlay"];
                    graphicsOverlay.Graphics.Clear();
                    graphicsOverlay.Graphics.Add(mapPinGraphic);
                    graphicsOverlay.IsVisible = true;
                    break;
                case nameof(ViewModel.CurrentState):
                    UpdateUIForNewState();
                    break;
                case nameof(ViewModel.CurrentlyIdentifiedRoom):
                    if (ViewModel.CurrentlyIdentifiedRoom != null)
                    {
                        _identifiedFeatureOverlay.Graphics.Clear();

                        _identifiedFeatureOverlay.Graphics.Add(new Graphic(ViewModel.CurrentlyIdentifiedRoom.FeatureLocation));
                    }
                    else
                    {
                        _identifiedFeatureOverlay.Graphics.Clear();
                    }
                    break;
            }
        }

        private async Task ConfigureGraphicsOverlays()
        {
            // Configure identify overlay
            _identifiedFeatureOverlay = new GraphicsOverlay();

            var mapPin = await UIImage.FromBundle("MapPin").ToRuntimeImageAsync();
            var roomMarker = new PictureMarkerSymbol(mapPin);
            roomMarker.OffsetY = mapPin.Height * 0.65;

            _identifiedFeatureOverlay.Renderer = new SimpleRenderer(roomMarker);

            // Configure home location overlay
            _homeOverlay = new GraphicsOverlay();
            var homePin = await UIImage.FromBundle("HomePin").ToRuntimeImageAsync();
            var homeMarker = new PictureMarkerSymbol(homePin);
            homeMarker.OffsetY = homePin.Height * 0.65;

            _homeOverlay.Renderer = new SimpleRenderer(homeMarker);

            // configure route overlay
        }

        /// <summary>
        /// When view is double tapped, set flag so the tapped event doesn't fire
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private void MapView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            this._isViewDoubleTapped = true;
        }

        /// <summary>
        /// When view is tapped, clear the map of selection, close keyboard and bottom sheet
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // Wait for double tap to fire
            await Task.Delay(500);

            // If view has been double tapped, set tapped to handled and flag back to false
            // If view has been tapped just once clear the map of selection, close keyboard and bottom sheet
            if (this._isViewDoubleTapped == true)
            {
                e.Handled = true;
                this._isViewDoubleTapped = false;
            }
            else
            {
                // If route card is visible, do not dismiss route
                if (ViewModel.CurrentState == UIState.RouteFound)
                {
                    // Create a new Alert Controller
                    UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);

                    // Add Actions
                    actionSheetAlert.AddAction(UIAlertAction.Create("ClearExistingRouteButtonText".AsLocalized(), UIAlertActionStyle.Destructive,
                        (action) => ViewModel.ReturnToWaitingState()));

                    actionSheetAlert.AddAction(UIAlertAction.Create("KeepExistingRouteButtonText".AsLocalized(), UIAlertActionStyle.Default, null));

                    // Required for iPad - You must specify a source for the Action Sheet since it is
                    // displayed as a popover
                    UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
                    if (presentationPopover != null)
                    {
                        presentationPopover.SourceView = _bottomSheet.DisplayedContentView;
                        presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                    }

                    // Display the alert
                    this.PresentViewController(actionSheetAlert, true, null);
                }
                else
                {
                    // get the tap location in screen unit
                    var tapScreenPoint = e.Position;

                    var layer = this._mapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex];
                    var pixelTolerance = 10;
                    var returnPopupsOnly = false;
                    var maxResults = 1;

                    try
                    {
                        // Identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max result
                        IdentifyLayerResult idResults = await this._mapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResults);

                        ViewModel.IdentifyRoomFromLayerResult(idResults);
                    }
                    catch
                    {
                        // TODO - log error
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for user tapping the blue Current Location button
        /// </summary>
        /// <param name="sender">Sender control.</param>
        private void CurrentLocationButton_TouchUpInside(object sender, EventArgs e)
        {
            this._mapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Off;
            this._mapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Recenter;
            this._mapView.LocationDisplay.IsEnabled = true;

            this.ViewModel.CurrentViewpoint = new Viewpoint(_mapView.LocationDisplay.Location.Position, 150);
        }

        /// <summary>
        /// Set the current location as user moves around
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        private void MapView_LocationChanged(object sender, Location e) => ViewModel.CurrentUserLocation = e.Position;

        /// <summary>
        /// Gets the index of the table view row.
        /// </summary>
        /// <returns>The table view row index.</returns>
        /// <param name="rowValue">Row value.</param>
        /// <param name="tableSource">Table source.</param>
        /// <param name="section">TableView Section.</param>
        private NSIndexPath GetTableViewRowIndex(string rowValue, string[] tableSource, nint section)
        {
            var rowIndex = tableSource.Select((rowItem, index) => new { rowItem, index }).First(i => i.rowItem == rowValue).index;
            return NSIndexPath.FromRowSection(rowIndex, section);
        }

        /// <summary>
        /// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event args.</param>
        private void FloorsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            this.ViewModel.SelectedFloorLevel = e.SelectedItem;
            this.ViewModel.SetFloorVisibility(true);
        }

        /// <summary>
        /// When user taps on the home button, zoom them to the home location
        /// </summary>
        /// <param name="sender">Home button</param>
        private void Home_TouchUpInside(object sender, EventArgs e) => this.ViewModel.MoveToHomeLocation();

        // top right buttons
        private UIButton _settingsButton;
        private UIButton _homeButton;
        private UIButton _locationButton;
        private SimpleStackedButtonContainer _accessoryView;

        private NSLayoutConstraint[] _invariantConstraints;

        private SelfSizedTableView _innerFloorsTableView;
        private UIView _innerFloorsTableViewShadow; // shadow container needs to be hidden for stack layout to work

        private UIStackView _topRightStack;

        private UIVisualEffectView _topBlur;

        private Compass _compass;
        private MapView _mapView;

        private BottomSheetViewController _bottomSheet;

        // Card used for searching for features, searching for origins, and searching for destinations
        private LocationSearchCard _locationSearchCard;

        // Location search result components
        private LocationInfoCard _locationCard;

        // Route planning components
        private RoutePlanningCard _routeSearchView;

        // Route result components
        private RouteResultCard _routeResultView;

        // Not found card
        private LocationNotFoundCard _locationNotFoundCard;

        // Attribution image
        private UIButton _esriIcon;
        private UIButton _attributionImageButton;
        private UIStackView _attributionStack;
        private UIView _shadowedAttribution;

        private AttributionViewController _attributionController;

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            ConfigureBottomSheet();
        }

        private void ConfigureAttribution()
        {
            _attributionStack = new UIStackView { TranslatesAutoresizingMaskIntoConstraints = false, Axis = UILayoutConstraintAxis.Horizontal };
            _attributionStack.Alignment = UIStackViewAlignment.Trailing;
            _attributionStack.Spacing = 8;

            _attributionImageButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _attributionImageButton.SetImage(UIImage.FromBundle("information").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate), UIControlState.Normal);
            _attributionImageButton.TintColor = UIColor.SystemBackgroundColor;

            _esriIcon = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _esriIcon.SetImage(UIImage.FromBundle("esri"), UIControlState.Normal);
            _esriIcon.TintColor = UIColor.SystemBackgroundColor;
            _esriIcon.AdjustsImageWhenHighlighted = false;
            _esriIcon.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;

            _attributionStack.AddArrangedSubview(_esriIcon);
            _attributionStack.AddArrangedSubview(_attributionImageButton);

            // put mapview attribution directly above map so it is under accesory views
            _shadowedAttribution = _attributionStack.EncapsulateInShadowView();
            View.InsertSubviewAbove(_shadowedAttribution, _mapView);

            _attributionImageButton.TouchUpInside += Attribution_Tapped;

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _shadowedAttribution.BottomAnchor.ConstraintEqualTo(_bottomSheet.PanelTopAnchor, -8),
                _shadowedAttribution.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -8),
                _esriIcon.HeightAnchor.ConstraintEqualTo(22),
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

        private async void Attribution_Tapped(object sender, EventArgs e)
        {
            if (_attributionController == null)
            {
                _attributionController = new AttributionViewController(_mapView);
            }

            await PresentViewControllerAsync(new UINavigationController(_attributionController), true);
        }

        private void ConfigureBottomSheet()
        {
            _bottomSheet = new BottomSheetViewController(View);

            this.AddChildViewController(_bottomSheet);

            _bottomSheet.DidMoveToParentViewController(this);

            _locationCard = new LocationInfoCard(ViewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _routeResultView = new RouteResultCard(ViewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _locationSearchCard = new LocationSearchCard(ViewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = false,
                BackgroundColor = UIColor.Clear
            };

            _routeSearchView = new RoutePlanningCard(ViewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };

            _locationNotFoundCard = new LocationNotFoundCard(ViewModel)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };

            ConfigureAttribution();

            UIStackView _containerView = new IntrinsicContentSizedStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical
            };

            _containerView.AddArrangedSubview(_locationSearchCard);
            _containerView.AddArrangedSubview(_locationNotFoundCard);
            _containerView.AddArrangedSubview(_locationCard);
            _containerView.AddArrangedSubview(_routeSearchView);
            _containerView.AddArrangedSubview(_routeResultView);

            _bottomSheet.DisplayedContentView.AddSubview(_containerView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _containerView.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                _containerView.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor),
                _containerView.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor)
            });
        }

        public override void LoadView()
        {
            base.LoadView();
            this.ViewModel = new MapViewModel();

            this.View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _mapView = new MapView { TranslatesAutoresizingMaskIntoConstraints = false };

            _settingsButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _homeButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _locationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _topRightStack = new IntrinsicContentSizedStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical,
                Spacing = 8,
                Distribution = UIStackViewDistribution.EqualSpacing
            };

            _accessoryView = new SimpleStackedButtonContainer(new[] { _homeButton, _settingsButton, _locationButton })
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _innerFloorsTableView = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _innerFloorsTableView.Layer.CornerRadius = 8;
            _innerFloorsTableView.SeparatorColor = UIColor.SystemGrayColor;

            _homeButton.SetImage(UIImage.FromBundle("home"), UIControlState.Normal);
            _locationButton.SetImage(UIImage.FromBundle("gps-on"), UIControlState.Normal);
            _settingsButton.SetImage(UIImage.FromBundle("gear"), UIControlState.Normal);

            _compass = new Compass() { TranslatesAutoresizingMaskIntoConstraints = false };
            _compass.GeoView = _mapView;

            var accessoryShadowContainer = _accessoryView.EncapsulateInShadowView();

            _topBlur = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial));
            _topBlur.TranslatesAutoresizingMaskIntoConstraints = false;

            _innerFloorsTableView.BackgroundColor = UIColor.Clear;
            _innerFloorsTableView.BackgroundView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));
            _innerFloorsTableViewShadow = _innerFloorsTableView.EncapsulateInShadowView();

            View.AddSubviews(_mapView, _topRightStack, _topBlur);

            _topRightStack.AddArrangedSubview(accessoryShadowContainer);
            _topRightStack.AddArrangedSubview(_innerFloorsTableViewShadow);
            _topRightStack.AddArrangedSubview(_compass);

            _invariantConstraints = new NSLayoutConstraint[]
            {
                _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _mapView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                // top-right floating buttons
                _topRightStack.TopAnchor.ConstraintEqualTo(_topBlur.BottomAnchor, 8),
                _topRightStack.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -8),
                // compass sizing
                _compass.WidthAnchor.ConstraintEqualTo(48),
                _compass.HeightAnchor.ConstraintEqualTo(48),
                // right panel accessories
                accessoryShadowContainer.HeightAnchor.ConstraintEqualTo(_accessoryView.HeightAnchor),
                accessoryShadowContainer.WidthAnchor.ConstraintEqualTo(48),
                // floors view
                _innerFloorsTableViewShadow.WidthAnchor.ConstraintEqualTo(accessoryShadowContainer.WidthAnchor),
                _innerFloorsTableViewShadow.HeightAnchor.ConstraintLessThanOrEqualTo(240),
                // Top blur (to make handlebar and system area easy to see)
                _topBlur.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _topBlur.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _topBlur.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _topBlur.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor)
            };

            NSLayoutConstraint.ActivateConstraints(_invariantConstraints);
        }


        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            SetAttributionForCurrentState();
        }
    }
}
