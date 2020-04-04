namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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
        
        private SelfSizedTableView _autoSuggestionsTableView;

        private UISearchBar _locationBar;

        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _invariantConstraints;

        private SelfSizedTableView _innerFloorsTableView;

        private UIStackView _topRightStack;

        private UIVisualEffectView _topBlur;

        private Compass _compass;
        private MapView _mapView;

        private BottomSheetViewController _bottomSheet;

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

            _bottomSheet.DisplayedContentView.AddSubview(_locationBar);

            _bottomSheet.DisplayedContentView.AddSubview(_autoSuggestionsTableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _locationBar.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor, 8),
                _locationBar.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor, -8),
                _locationBar.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor),
                _autoSuggestionsTableView.TopAnchor.ConstraintEqualTo(_locationBar.BottomAnchor, 8),
                _autoSuggestionsTableView.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                _autoSuggestionsTableView.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor)
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

            _topRightStack = new UIStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical,
                Distribution = UIStackViewDistribution.EqualSpacing,
                Spacing = 8
            };

            var accesoryView = new SimpleStackedButtonContainer(new[] { _homeButton, _settingsButton, _locationButton })
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
            _locationBar.SearchBarStyle = UISearchBarStyle.Prominent;

            _homeButton.SetImage(UIImage.FromBundle("Home"), UIControlState.Normal);
            _locationButton.SetImage(UIImage.FromBundle("Home"), UIControlState.Normal);
            _settingsButton.SetImage(UIImage.FromBundle("Settings"), UIControlState.Normal);

            _compass = new Compass() { TranslatesAutoresizingMaskIntoConstraints = false };
            _compass.GeoView = _mapView;

            var accessoryShadowContainer = accesoryView.EncapsulateInShadowView();

            _topBlur = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterial));
            _topBlur.TranslatesAutoresizingMaskIntoConstraints = false;

            _innerFloorsTableView.BackgroundColor = UIColor.Clear;
            _innerFloorsTableView.BackgroundView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));
            var floorsTableShadowContainer = _innerFloorsTableView.EncapsulateInShadowView();

            View.AddSubviews(_mapView, _topRightStack, _topBlur);
            
            _topRightStack.AddArrangedSubview(accessoryShadowContainer);
            _topRightStack.AddArrangedSubview(_compass);
            _topRightStack.AddArrangedSubview(floorsTableShadowContainer);

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
                accessoryShadowContainer.HeightAnchor.ConstraintEqualTo(accesoryView.HeightAnchor, 1, 16),
                accessoryShadowContainer.WidthAnchor.ConstraintEqualTo(48),
                // floors view
                floorsTableShadowContainer.WidthAnchor.ConstraintEqualTo(accessoryShadowContainer.WidthAnchor),
                floorsTableShadowContainer.HeightAnchor.ConstraintLessThanOrEqualTo(240),
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

        private void _mapView_ViewpointChanged(object sender, EventArgs e)
        {
            var rotation = _mapView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.Rotation;
            if (rotation == null) return;
            if (rotation == 0)
            {
                UIView.Animate(0.5, () =>
                {
                    _compass.Hidden = true;
                });
                
            }
            else
            {
                UIView.Animate(0.5, () =>
                {
                    _compass.Hidden = false;
                });
                
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
