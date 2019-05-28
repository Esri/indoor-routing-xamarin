// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace IndoorRouting.iOS
{
    [Register ("MapViewController")]
    partial class MapViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView AutosuggestionsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint ButtonBottomConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView ContactCardView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton CurrentLocationButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton DirectionsButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint FloorPickerBottomConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView FloorsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint HeightConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem HomeButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISearchBar LocationSearchBar { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel MainLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        Esri.ArcGISRuntime.UI.Controls.MapView MapView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView RouteCard { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView RouteTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIToolbar SearchToolbar { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel SecondaryLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem SettingsButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel WalkTimeLabel { get; set; }

        [Action ("CurrentLocationButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void CurrentLocationButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("Home_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void Home_TouchUpInside (UIKit.UIBarButtonItem sender);

        void ReleaseDesignerOutlets ()
        {
            if (AutosuggestionsTableView != null) {
                AutosuggestionsTableView.Dispose ();
                AutosuggestionsTableView = null;
            }

            if (ButtonBottomConstraint != null) {
                ButtonBottomConstraint.Dispose ();
                ButtonBottomConstraint = null;
            }

            if (ContactCardView != null) {
                ContactCardView.Dispose ();
                ContactCardView = null;
            }

            if (CurrentLocationButton != null) {
                CurrentLocationButton.Dispose ();
                CurrentLocationButton = null;
            }

            if (DirectionsButton != null) {
                DirectionsButton.Dispose ();
                DirectionsButton = null;
            }

            if (FloorPickerBottomConstraint != null) {
                FloorPickerBottomConstraint.Dispose ();
                FloorPickerBottomConstraint = null;
            }

            if (FloorsTableView != null) {
                FloorsTableView.Dispose ();
                FloorsTableView = null;
            }

            if (HeightConstraint != null) {
                HeightConstraint.Dispose ();
                HeightConstraint = null;
            }

            if (HomeButton != null) {
                HomeButton.Dispose ();
                HomeButton = null;
            }

            if (LocationSearchBar != null) {
                LocationSearchBar.Dispose ();
                LocationSearchBar = null;
            }

            if (MainLabel != null) {
                MainLabel.Dispose ();
                MainLabel = null;
            }

            if (MapView != null) {
                MapView.Dispose ();
                MapView = null;
            }

            if (RouteCard != null) {
                RouteCard.Dispose ();
                RouteCard = null;
            }

            if (RouteTableView != null) {
                RouteTableView.Dispose ();
                RouteTableView = null;
            }

            if (SearchToolbar != null) {
                SearchToolbar.Dispose ();
                SearchToolbar = null;
            }

            if (SecondaryLabel != null) {
                SecondaryLabel.Dispose ();
                SecondaryLabel = null;
            }

            if (SettingsButton != null) {
                SettingsButton.Dispose ();
                SettingsButton = null;
            }

            if (WalkTimeLabel != null) {
                WalkTimeLabel.Dispose ();
                WalkTimeLabel = null;
            }
        }
    }
}