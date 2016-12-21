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
        UIKit.UITableView FloorsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        Esri.ArcGISRuntime.UI.Controls.MapView MapView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (FloorsTableView != null) {
                FloorsTableView.Dispose ();
                FloorsTableView = null;
            }

            if (MapView != null) {
                MapView.Dispose ();
                MapView = null;
            }
        }
    }
}