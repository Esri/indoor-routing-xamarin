// <copyright file="SettingsController.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorRouting.iOS
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using UIKit;

    /// <summary>
    /// Controller handles the UI and logic for the Settings screen
    /// </summary>
    internal partial class SettingsController : UITableViewController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorRouting.iOS.SettingsController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        private SettingsController(IntPtr handle) : base(handle)
        {
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;

            // Set the label for the home location from settings
            this.HomeLocationLabel.Text = AppSettings.CurrentSettings.HomeLocation;

            // Set the current location switch
            this.CurrentLocationSwitch.On = AppSettings.CurrentSettings.IsLocationServicesEnabled;

            base.ViewWillAppear(animated);
        }

        /// <summary>
        /// Handles when user toggles the Current Location switch on/off
        /// </summary>
        /// <param name="sender">Sender control.</param>
        partial void CurrentLocationSwitchValueChanged(UISwitch sender)
        {
            AppSettings.CurrentSettings.IsLocationServicesEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }
    }
}
