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
    [Register ("RouteController")]
    partial class RouteController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView AutosuggestionsTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISearchBar EndSearchBar { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton RouteButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView RouteView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISearchBar StartSearchBar { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (AutosuggestionsTableView != null) {
                AutosuggestionsTableView.Dispose ();
                AutosuggestionsTableView = null;
            }

            if (EndSearchBar != null) {
                EndSearchBar.Dispose ();
                EndSearchBar = null;
            }

            if (RouteButton != null) {
                RouteButton.Dispose ();
                RouteButton = null;
            }

            if (RouteView != null) {
                RouteView.Dispose ();
                RouteView = null;
            }

            if (StartSearchBar != null) {
                StartSearchBar.Dispose ();
                StartSearchBar = null;
            }
        }
    }
}