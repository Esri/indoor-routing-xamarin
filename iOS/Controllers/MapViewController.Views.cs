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

        private UIView _locationCard;
        private UIButton _startDirectionsFromLocationCardButton;
        private UIButton _closeLocationCardButton;
        private UILabel _locationCardPrimaryLabel;
        private UILabel _locationCardSecondaryLabel;

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

            _bottomSheet.DisplayedContentView.AddSubview(_locationBar);

            _bottomSheet.DisplayedContentView.AddSubview(_autoSuggestionsTableView);

            _bottomSheet.DisplayedContentView.AddSubview(_locationCard);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _locationBar.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor, 8),
                _locationBar.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor, -8),
                _locationBar.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor),
                _autoSuggestionsTableView.TopAnchor.ConstraintEqualTo(_locationBar.BottomAnchor, 8),
                _autoSuggestionsTableView.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                _autoSuggestionsTableView.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor),
                _locationCard.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor),
                _locationCard.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor),
                _locationCard.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor),
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
            _startDirectionsFromLocationCardButton.Layer.CornerRadius = 8;

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
            _topRightStack.AddArrangedSubview(floorsTableShadowContainer);
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
