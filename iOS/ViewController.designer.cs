// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace IndoorNavigation.iOS
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        UIKit.UIButton Button { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton CurrentLocation { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView FloorsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton Home { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton InvisibleButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        Esri.ArcGISRuntime.UI.Controls.MapView MapView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton Settings { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (CurrentLocation != null) {
                CurrentLocation.Dispose ();
                CurrentLocation = null;
            }

            if (FloorsTableView != null) {
                FloorsTableView.Dispose ();
                FloorsTableView = null;
            }

            if (Home != null) {
                Home.Dispose ();
                Home = null;
            }

            if (InvisibleButton != null) {
                InvisibleButton.Dispose ();
                InvisibleButton = null;
            }

            if (MapView != null) {
                MapView.Dispose ();
                MapView = null;
            }

            if (Settings != null) {
                Settings.Dispose ();
                Settings = null;
            }
        }
    }
}