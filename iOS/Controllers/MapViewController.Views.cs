namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CoreGraphics;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Location;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views;
    using Esri.ArcGISRuntime.Symbology;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
    using Esri.ArcGISRuntime.Toolkit.UI.Controls;
    using Esri.ArcGISRuntime.UI;
    using Esri.ArcGISRuntime.UI.Controls;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
    {
        // top right buttons
        private UIButton _settingsButton;
        private UIButton _homeButton;
        private UIButton _locationButton;
        private SimpleStackedButtonContainer _accessoryView;
        
        private SelfSizedTableView _autoSuggestionsTableView;

        private UISearchBar _locationBar;

        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _invariantConstraints;

        private SelfSizedTableView _innerFloorsTableView;
        private UIView _innerFloorsTableViewShadow; // shadow container needs to be hidden for stack layout to work

        private UIStackView _topRightStack;

        private UIVisualEffectView _topBlur;

        private Compass _compass;
        private MapView _mapView;

        private BottomSheetViewController _bottomSheet;

        // Location search result components
        private UIView _locationCard;
        private UIButton _startDirectionsFromLocationCardButton;
        private UIButton _closeLocationCardButton;
        private UILabel _locationCardPrimaryLabel;
        private UILabel _locationCardSecondaryLabel;

        // Route planning components
        private UIView _routeSearchView;
        private UISearchBar _startSearchBar;
        private UISearchBar _endSearchBar;
        private UIButton _searchRouteButton;
        private UILabel _searchStartLabel;
        private UILabel _searchEndLabel;
        private UIButton _cancelRouteSearchButton;
        private UILabel _routeSearchHeader;
        private UIButton _swapOriginDestinationButton;

        // Route result components
        private UIView _routeResultView;
        private SelfSizedTableView _routeResultStopsView;
        private UIImageView _routeTravelModeImage;
        private UILabel _walkTimeLabel;
        private UIButton _clearRouteResultButton;
        private UILabel _routeResultHeader;

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

            ConfigureLocationCard();

            ConfigureRouteSearchCard();

            ConfigureRouteResultView();

            ConfigureAttribution();

            UIStackView _containerView = new IntrinsicContentSizedStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical
            };

            _containerView.AddArrangedSubview(_locationBar);
            _containerView.AddArrangedSubview(_locationCard);
            _containerView.AddArrangedSubview(_routeSearchView);
            _containerView.AddArrangedSubview(_routeResultView);
            _containerView.AddArrangedSubview(_autoSuggestionsTableView);

            _bottomSheet.DisplayedContentView.AddSubview(_containerView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _containerView.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                _containerView.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor),
                _containerView.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor)
            });

            _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);

            
        }

        private void ConfigureRouteResultView()
        {
            _routeResultView = new UIView { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = true };
            _routeResultStopsView = new SelfSizedTableView { TranslatesAutoresizingMaskIntoConstraints = false };
            _routeResultStopsView.TableFooterView = null;
            _routeResultStopsView.ScrollEnabled = false;
            _routeResultStopsView.BackgroundColor = UIColor.Clear;
            _routeResultStopsView.ScrollEnabled = false;
            _routeResultStopsView.AllowsSelection = false;

            _routeTravelModeImage = new UIImageView(UIImage.FromBundle("walking")) { TranslatesAutoresizingMaskIntoConstraints = false };
            _routeTravelModeImage.TintColor = UIColor.LabelColor;
            _routeTravelModeImage.ContentMode = UIViewContentMode.ScaleAspectFit;

            _walkTimeLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };

            _clearRouteResultButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _routeResultHeader = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "Route" };
            _routeResultHeader.Font = UIFont.BoldSystemFontOfSize(28);
            _routeResultHeader.TextColor = UIColor.LabelColor;

            _clearRouteResultButton.TouchUpInside += _clearRouteResultButton_TouchUpInside;

            _routeResultView.AddSubviews(_routeResultStopsView, _routeTravelModeImage, _walkTimeLabel, _clearRouteResultButton, _routeResultHeader);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // result header
                _routeResultHeader.TopAnchor.ConstraintEqualTo(_routeResultView.TopAnchor, 8),
                _routeResultHeader.LeadingAnchor.ConstraintEqualTo(_routeResultView.LeadingAnchor, 8),
                _routeResultHeader.TrailingAnchor.ConstraintEqualTo(_clearRouteResultButton.LeadingAnchor, -8),
                //clear button
                _clearRouteResultButton.TrailingAnchor.ConstraintEqualTo(_routeResultView.TrailingAnchor, -8),
                _clearRouteResultButton.CenterYAnchor.ConstraintEqualTo(_routeResultHeader.CenterYAnchor),
                _clearRouteResultButton.WidthAnchor.ConstraintEqualTo(32),
                _clearRouteResultButton.HeightAnchor.ConstraintEqualTo(32),
                // stops view
                _routeResultStopsView.LeadingAnchor.ConstraintEqualTo(_routeResultView.LeadingAnchor),
                _routeResultStopsView.TopAnchor.ConstraintEqualTo(_walkTimeLabel.BottomAnchor, 8),
                _routeResultStopsView.TrailingAnchor.ConstraintEqualTo(_routeResultView.TrailingAnchor, -8),
                // image
                _routeTravelModeImage.LeadingAnchor.ConstraintEqualTo(_routeResultView.LeadingAnchor, 8),
                _routeTravelModeImage.TopAnchor.ConstraintEqualTo(_walkTimeLabel.TopAnchor),
                _routeTravelModeImage.BottomAnchor.ConstraintEqualTo(_walkTimeLabel.BottomAnchor),
                _routeTravelModeImage.WidthAnchor.ConstraintEqualTo(32),
                // walk time label
                _walkTimeLabel.TopAnchor.ConstraintEqualTo(_routeResultHeader.BottomAnchor, 8),
                _walkTimeLabel.LeadingAnchor.ConstraintEqualTo(_routeTravelModeImage.TrailingAnchor, 8),
                //
                _routeResultView.BottomAnchor.ConstraintEqualTo(_routeResultStopsView.BottomAnchor, 8)
            });
        }

        private void SetLocationCardHidden(bool isHidden)
        {
            _locationCard.Hidden = isHidden;
            _locationBar.Hidden = !isHidden;

            _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
        }

        private void ConfigureRouteSearchCard()
        {
            _routeSearchView = new UIView { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = true };

            _startSearchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            _startSearchBar.BackgroundImage = new UIImage();
            _startSearchBar.Placeholder = "Origin";
            _startSearchBar.SearchBarStyle = UISearchBarStyle.Minimal;

            _endSearchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            _endSearchBar.BackgroundImage = new UIImage();
            _endSearchBar.Placeholder = "Destination";
            _endSearchBar.SearchBarStyle = UISearchBarStyle.Minimal;

            _searchRouteButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _searchRouteButton.SetTitle("Find Route", UIControlState.Normal);
            _searchRouteButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _searchRouteButton.BackgroundColor = UIColor.SystemBlueColor;
            _searchRouteButton.Layer.CornerRadius = 8;

            _cancelRouteSearchButton = new CloseButton { TranslatesAutoresizingMaskIntoConstraints = false };

            _searchStartLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "From:" };
            _searchEndLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "To:" };

            _routeSearchHeader = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "Directions" };
            _routeSearchHeader.TextColor = UIColor.LabelColor;
            _routeSearchHeader.Font = UIFont.BoldSystemFontOfSize(24);

            // swap origin and destination button
            _swapOriginDestinationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _swapOriginDestinationButton.SetImage(UIImage.FromBundle("arrow-up-down"), UIControlState.Normal);
            _swapOriginDestinationButton.TintColor = UIColor.LabelColor;

            // Initial event setup
            _startSearchBar.TextChanged += LocationSearch_TextChanged;
            _startSearchBar.SearchButtonClicked += LocationSearch_SearchButtonClicked;
            _startSearchBar.OnEditingStarted += _locationBar_OnEditingStarted;
            _endSearchBar.TextChanged += LocationSearch_TextChanged;
            _endSearchBar.SearchButtonClicked += LocationSearch_SearchButtonClicked;
            _endSearchBar.OnEditingStarted += _locationBar_OnEditingStarted;

            _searchRouteButton.TouchUpInside += RouteSearch_TouchUpInside;

            _cancelRouteSearchButton.TouchUpInside += _cancelRouteSearchButton_TouchUpInside;

            _swapOriginDestinationButton.TouchUpInside += _swapOriginDestinationButton_TouchUpInside;

            _routeSearchView.AddSubviews(_startSearchBar, _endSearchBar, _searchRouteButton, _searchStartLabel, _searchEndLabel, _cancelRouteSearchButton, _routeSearchHeader, _swapOriginDestinationButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // label
                _routeSearchHeader.TopAnchor.ConstraintEqualTo(_routeSearchView.TopAnchor, 8),
                _routeSearchHeader.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _routeSearchHeader.TrailingAnchor.ConstraintEqualTo(_cancelRouteSearchButton.LeadingAnchor, -8),
                // close button
                _cancelRouteSearchButton.CenterYAnchor.ConstraintEqualTo(_routeSearchHeader.CenterYAnchor),
                _cancelRouteSearchButton.TrailingAnchor.ConstraintEqualTo(_routeSearchView.TrailingAnchor, -8),
                _cancelRouteSearchButton.HeightAnchor.ConstraintEqualTo(32),
                _cancelRouteSearchButton.WidthAnchor.ConstraintEqualTo(32),
                // labels
                _searchStartLabel.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchStartLabel.CenterYAnchor.ConstraintEqualTo(_startSearchBar.CenterYAnchor),
                _searchEndLabel.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchEndLabel.CenterYAnchor.ConstraintEqualTo(_endSearchBar.CenterYAnchor),
                _searchEndLabel.TrailingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                // search bars
                _startSearchBar.LeadingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                _startSearchBar.TopAnchor.ConstraintEqualTo(_routeSearchHeader.BottomAnchor, 8 - 4),
                _startSearchBar.TrailingAnchor.ConstraintEqualTo(_routeSearchView.TrailingAnchor),
                _endSearchBar.LeadingAnchor.ConstraintEqualTo(_startSearchBar.LeadingAnchor),
                _endSearchBar.TrailingAnchor.ConstraintEqualTo(_startSearchBar.TrailingAnchor),
                _endSearchBar.TopAnchor.ConstraintEqualTo(_startSearchBar.BottomAnchor, -8 - 4),
                // search button
                _searchRouteButton.TrailingAnchor.ConstraintEqualTo(_swapOriginDestinationButton.LeadingAnchor, -8),
                _searchRouteButton.TopAnchor.ConstraintEqualTo(_endSearchBar.BottomAnchor, -8 + 4),
                _searchRouteButton.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchRouteButton.HeightAnchor.ConstraintEqualTo(44),
                // swap origin and destinations button
                _swapOriginDestinationButton.HeightAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.WidthAnchor.ConstraintEqualTo(44),
                _swapOriginDestinationButton.TrailingAnchor.ConstraintEqualTo(_routeSearchView.TrailingAnchor, -8),
                _swapOriginDestinationButton.CenterYAnchor.ConstraintEqualTo(_searchRouteButton.CenterYAnchor),
                // update bottom size
                _routeSearchView.BottomAnchor.ConstraintEqualTo(_searchRouteButton.BottomAnchor, 8)
            });
        }

        private void _swapOriginDestinationButton_TouchUpInside(object sender, EventArgs e)
        {
            string oldOrigin = _startSearchBar.Text;
            string oldDestination = _endSearchBar.Text;

            _startSearchBar.Text = oldDestination;
            _endSearchBar.Text = oldOrigin;
        }

        private void ConfigureLocationCard()
        {
            _locationCard = new UIView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _startDirectionsFromLocationCardButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _startDirectionsFromLocationCardButton.SetTitle("Directions", UIControlState.Normal);
            _startDirectionsFromLocationCardButton.BackgroundColor = UIColor.SystemBlueColor;
            _startDirectionsFromLocationCardButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _startDirectionsFromLocationCardButton.SetTitleColor(UIColor.SystemGrayColor, UIControlState.Disabled);
            _startDirectionsFromLocationCardButton.Layer.CornerRadius = 8;

            // Handle searching for directions
            _startDirectionsFromLocationCardButton.TouchUpInside += _startDirectionsFromLocationCardButton_TouchUpInside;

            _closeLocationCardButton = new CloseButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            

            _locationCardPrimaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            _locationCardPrimaryLabel.Font = _locationCardPrimaryLabel.Font.WithSize(18);

            _locationCardSecondaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor
            };

            _locationCard.AddSubviews(_startDirectionsFromLocationCardButton, _closeLocationCardButton, _locationCardPrimaryLabel, _locationCardSecondaryLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _locationCardPrimaryLabel.LeadingAnchor.ConstraintEqualTo(_locationCard.LeadingAnchor, 8),
                _locationCardPrimaryLabel.TopAnchor.ConstraintEqualTo(_locationCard.TopAnchor, 8),
                _locationCardPrimaryLabel.TrailingAnchor.ConstraintEqualTo(_closeLocationCardButton.LeadingAnchor, -8),
                _locationCardSecondaryLabel.LeadingAnchor.ConstraintEqualTo(_locationCardPrimaryLabel.LeadingAnchor),
                _locationCardSecondaryLabel.TrailingAnchor.ConstraintEqualTo(_locationCardPrimaryLabel.TrailingAnchor),
                _locationCardSecondaryLabel.TopAnchor.ConstraintEqualTo(_locationCardPrimaryLabel.BottomAnchor, 8),
                _closeLocationCardButton.TopAnchor.ConstraintEqualTo(_locationCard.TopAnchor, 8),
                _closeLocationCardButton.TrailingAnchor.ConstraintEqualTo(_locationCard.TrailingAnchor, -8),
                _closeLocationCardButton.WidthAnchor.ConstraintEqualTo(32),
                _closeLocationCardButton.HeightAnchor.ConstraintEqualTo(32),
                _startDirectionsFromLocationCardButton.LeadingAnchor.ConstraintEqualTo(_locationCard.LeadingAnchor, 8),
                _startDirectionsFromLocationCardButton.TrailingAnchor.ConstraintEqualTo(_locationCard.TrailingAnchor, -8),
                _startDirectionsFromLocationCardButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_locationCardSecondaryLabel.BottomAnchor, 8),
                _startDirectionsFromLocationCardButton.TopAnchor.ConstraintGreaterThanOrEqualTo(_closeLocationCardButton.BottomAnchor, 8),
                _startDirectionsFromLocationCardButton.HeightAnchor.ConstraintEqualTo(44),
                _locationCard.BottomAnchor.ConstraintEqualTo(_startDirectionsFromLocationCardButton.BottomAnchor, 8)
            });

            // Handle closing location card.
            if (_closeLocationCardButton != null)
            {
                this._closeLocationCardButton.TouchUpInside += _closeLocationCardButton_TouchUpInside;
            }
            // Check settings
            _startDirectionsFromLocationCardButton.Enabled = AppSettings.CurrentSettings.IsRoutingEnabled;
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

            _autoSuggestionsTableView = new SelfSizedTableView { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = true };
            _autoSuggestionsTableView.BackgroundColor = UIColor.Clear;
            _autoSuggestionsTableView.SeparatorColor = UIColor.SystemGrayColor;

            _innerFloorsTableView = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _innerFloorsTableView.Layer.CornerRadius = 8;
            _innerFloorsTableView.SeparatorColor = UIColor.SystemGrayColor;

            _locationBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            _locationBar.BackgroundImage = new UIImage();
            _locationBar.Placeholder = "Search for a place or address";
            _locationBar.UserInteractionEnabled = true;
            _locationBar.SearchBarStyle = UISearchBarStyle.Minimal;

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

            _regularWidthConstraints = new NSLayoutConstraint[]
            {
                // card container
            };

            _compactWidthConstraints = new NSLayoutConstraint[]
            {
                
                // card container
            };

            NSLayoutConstraint.ActivateConstraints(_invariantConstraints);

            ApplyConstraintsForSizeClass();

            // Defined in Helpers/ViewExtensions
        }

        private void SetAutoSuggestHidden(bool isHidden)
        {
            _autoSuggestionsTableView.Hidden = isHidden;

            _locationBar.ShowsCancelButton = !isHidden;

            if (isHidden)
            {
                _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
            }
            else
            {
                _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.full);
            }
        }

        private void ApplyConstraintsForSizeClass()
        {
            NSLayoutConstraint.DeactivateConstraints(_compactWidthConstraints);
            NSLayoutConstraint.DeactivateConstraints(_regularWidthConstraints);

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_regularWidthConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_compactWidthConstraints);
            }
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            SetAttributionForCurrentState();

            ApplyConstraintsForSizeClass();
        }
    }
}
