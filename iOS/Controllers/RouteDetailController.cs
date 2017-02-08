// <copyright file="RouteDetailController.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation.iOS
{
    using System;
    using UIKit;

    /// <summary>
    /// Route detail controller.
    /// </summary>
    public partial class RouteDetailController : UITableViewController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.RouteDetailController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        public RouteDetailController(IntPtr handle) : base(handle)
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
            base.ViewWillAppear(animated);
        }
    }
}
