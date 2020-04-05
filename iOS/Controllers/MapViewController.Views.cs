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

        // Route result components
        private UIView _routeResultView;
        private SelfSizedTableView _routeResultStopsView;
        private UIImageView _routeTravelModeImage;
        private UILabel _walkTimeLabel;

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            ConfigureBottomSheet();
        }

        private void ConfigureBottomSheet()
        {
            _bottomSheet = new BottomSheetViewController(View);

            this.AddChildViewController(_bottomSheet);

            _bottomSheet.DidMoveToParentViewController(this);

            ConfigureLocationCard();

            ConfigureRouteSearchCard();

            ConfigureRouteResultView();

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
        }

        private void ConfigureRouteResultView()
        {
            _routeResultView = new UIView { TranslatesAutoresizingMaskIntoConstraints = false, Hidden = true };
            _routeResultStopsView = new SelfSizedTableView { TranslatesAutoresizingMaskIntoConstraints = false };
            _routeResultStopsView.TableFooterView = null;
            _routeResultStopsView.ScrollEnabled = false;
            _routeResultStopsView.BackgroundColor = UIColor.Clear;

            _routeTravelModeImage = new UIImageView(UIImage.FromBundle("Walk")) { TranslatesAutoresizingMaskIntoConstraints = false };
            _routeTravelModeImage.TintColor = UIColor.FromName("AccessoryButtonColor");

            _walkTimeLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };
            _walkTimeLabel.TextAlignment = UITextAlignment.Center;

            _routeResultView.AddSubviews(_routeResultStopsView, _routeTravelModeImage, _walkTimeLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _routeResultStopsView.LeadingAnchor.ConstraintEqualTo(_routeResultView.LeadingAnchor, 8),
                _routeResultStopsView.TopAnchor.ConstraintEqualTo(_routeResultView.TopAnchor, 8),
                _routeResultStopsView.TrailingAnchor.ConstraintEqualTo(_walkTimeLabel.LeadingAnchor, -8),
                _routeTravelModeImage.TopAnchor.ConstraintEqualTo(_routeResultView.TopAnchor, 8),
                _routeTravelModeImage.HeightAnchor.ConstraintEqualTo(44),
                _routeTravelModeImage.CenterXAnchor.ConstraintEqualTo(_walkTimeLabel.CenterXAnchor),
                _walkTimeLabel.TrailingAnchor.ConstraintEqualTo(_routeResultView.TrailingAnchor, -8),
                _walkTimeLabel.TopAnchor.ConstraintEqualTo(_routeTravelModeImage.BottomAnchor, 8),
                _walkTimeLabel.WidthAnchor.ConstraintEqualTo(80),
                _routeResultView.BottomAnchor.ConstraintEqualTo(_routeResultStopsView.BottomAnchor, 8)
            });
        }

        private void SetLocationCardHidden(bool isHidden)
        {
            _locationCard.Hidden = isHidden;
            _locationBar.Hidden = !isHidden;

            if (isHidden)
            {
                _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.minimized);
            }
            else
            {
                _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
            }
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
            _searchRouteButton.SetTitle("Route Me", UIControlState.Normal);
            _searchRouteButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _searchRouteButton.BackgroundColor = UIColor.SystemBlueColor;
            _searchRouteButton.Layer.CornerRadius = 8;

            _searchStartLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "Start" };
            _searchEndLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false, Text = "End" };

            // Initial event setup
            _startSearchBar.TextChanged += LocationSearch_TextChanged;
            _startSearchBar.SearchButtonClicked += LocationSearch_SearchButtonClicked;
            _startSearchBar.OnEditingStarted += _locationBar_OnEditingStarted;
            _endSearchBar.TextChanged += LocationSearch_TextChanged;
            _endSearchBar.SearchButtonClicked += LocationSearch_SearchButtonClicked;
            _endSearchBar.OnEditingStarted += _locationBar_OnEditingStarted;

            _searchRouteButton.TouchUpInside += RouteSearch_TouchUpInside;

            _routeSearchView.AddSubviews(_startSearchBar, _endSearchBar, _searchRouteButton, _searchStartLabel, _searchEndLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                // labels
                _searchStartLabel.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchStartLabel.CenterYAnchor.ConstraintEqualTo(_startSearchBar.CenterYAnchor),
                _searchEndLabel.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchEndLabel.CenterYAnchor.ConstraintEqualTo(_endSearchBar.CenterYAnchor),
                _searchEndLabel.TrailingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                // search bars
                _startSearchBar.LeadingAnchor.ConstraintEqualTo(_searchStartLabel.TrailingAnchor),
                _startSearchBar.TopAnchor.ConstraintEqualTo(_routeSearchView.TopAnchor, 8),
                _startSearchBar.TrailingAnchor.ConstraintEqualTo(_routeSearchView.TrailingAnchor),
                _endSearchBar.LeadingAnchor.ConstraintEqualTo(_startSearchBar.LeadingAnchor),
                _endSearchBar.TrailingAnchor.ConstraintEqualTo(_startSearchBar.TrailingAnchor),
                _endSearchBar.TopAnchor.ConstraintEqualTo(_startSearchBar.BottomAnchor, -12),
                // search button
                _searchRouteButton.TrailingAnchor.ConstraintEqualTo(_routeSearchView.TrailingAnchor, -8),
                _searchRouteButton.TopAnchor.ConstraintEqualTo(_endSearchBar.BottomAnchor, -4),
                _searchRouteButton.LeadingAnchor.ConstraintEqualTo(_routeSearchView.LeadingAnchor, 8),
                _searchRouteButton.HeightAnchor.ConstraintEqualTo(44),
                // update bottom size
                _routeSearchView.BottomAnchor.ConstraintEqualTo(_searchRouteButton.BottomAnchor, 8)
            });
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

            _closeLocationCardButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            _closeLocationCardButton.BackgroundColor = UIColor.SystemGray5Color;
            _closeLocationCardButton.Layer.CornerRadius = 22;
            _closeLocationCardButton.SetImage(UIImage.GetSystemImage("xmark"), UIControlState.Normal);

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
                _closeLocationCardButton.WidthAnchor.ConstraintEqualTo(44),
                _closeLocationCardButton.HeightAnchor.ConstraintEqualTo(44),
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

            _innerFloorsTableView = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            _innerFloorsTableView.Layer.CornerRadius = 8;

            _locationBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            _locationBar.BackgroundImage = new UIImage();
            _locationBar.Placeholder = "Search for a place or address";
            _locationBar.UserInteractionEnabled = true;
            _locationBar.SearchBarStyle = UISearchBarStyle.Minimal;

            _homeButton.SetImage(UIImage.FromBundle("Home"), UIControlState.Normal);
            _locationButton.SetImage(UIImage.FromBundle("CurrentLocation"), UIControlState.Normal);
            _settingsButton.SetImage(UIImage.FromBundle("Settings"), UIControlState.Normal);

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
                _topRightStack.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -8),
                // compass sizing
                _compass.WidthAnchor.ConstraintEqualTo(48),
                _compass.HeightAnchor.ConstraintEqualTo(48),
                // right panel accessories
                accessoryShadowContainer.HeightAnchor.ConstraintEqualTo(_accessoryView.HeightAnchor, 1, 16),
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
            ApplyConstraintsForSizeClass();
        }
    }
}
