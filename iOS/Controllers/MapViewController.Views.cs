namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views;
    using Esri.ArcGISRuntime.Toolkit.UI.Controls;
    using Esri.ArcGISRuntime.UI.Controls;
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

            _locationCard = new LocationInfoCard()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _routeResultView = new RouteResultCard()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true,
                BackgroundColor = UIColor.Clear
            };

            _locationSearchCard = new LocationSearchCard()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = false,
                BackgroundColor = UIColor.Clear
            };

            _routeSearchView = new RoutePlanningCard()
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

            _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
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
