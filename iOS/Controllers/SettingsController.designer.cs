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
        UIKit.UILabel HomeLocationLabel { get; set; }

        [Action ("CurrentLocationSwitchValueChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void CurrentLocationSwitchValueChanged (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (CurrentLocationSwitch != null) {
                CurrentLocationSwitch.Dispose ();
                CurrentLocationSwitch = null;
            }

            if (HomeLocationLabel != null) {
                HomeLocationLabel.Dispose ();
                HomeLocationLabel = null;
            }
        }
    }
}