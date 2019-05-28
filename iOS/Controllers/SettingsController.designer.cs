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
    [Register ("SettingsController")]
    partial class SettingsController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch CurrentLocationSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch EnableRoutingSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel HomeLocationLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView SettingsTableView { get; set; }

        [Action ("CurrentLocationSwitchValueChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void CurrentLocationSwitchValueChanged (UIKit.UISwitch sender);

        [Action ("EnableRoutingSwitchValueChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void EnableRoutingSwitchValueChanged (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (CurrentLocationSwitch != null) {
                CurrentLocationSwitch.Dispose ();
                CurrentLocationSwitch = null;
            }

            if (EnableRoutingSwitch != null) {
                EnableRoutingSwitch.Dispose ();
                EnableRoutingSwitch = null;
            }

            if (HomeLocationLabel != null) {
                HomeLocationLabel.Dispose ();
                HomeLocationLabel = null;
            }

            if (SettingsTableView != null) {
                SettingsTableView.Dispose ();
                SettingsTableView = null;
            }
        }
    }
}