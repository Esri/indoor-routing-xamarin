// <copyright file="SettingsController.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation.iOS
{
    using System;
    using UIKit;

    /// <summary>
    /// Controller handles the UI and logic for the Settings screen
    /// </summary>
    internal partial class SettingsController : UITableViewController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.SettingsController"/> class.
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

            base.ViewWillAppear(animated);
        }
    }
}