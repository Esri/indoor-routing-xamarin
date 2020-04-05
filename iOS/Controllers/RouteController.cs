// <copyright file="RouteController.cs" company="Esri, Inc">
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
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Route controller.
    /// </summary>
    internal partial class RouteController : UIViewController
    {
        /// <summary>
        /// The start search bar flag.
        /// </summary>
        private bool startSearchBarFlag;

        /// <summary>
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        public RouteController(IntPtr handle) : base(handle)
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

        /// <summary>
        /// Overrides the behavior of the controller once the view has loaded
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.EndSearchBar.SearchButtonClicked += (sender, e) =>
            {
                AutosuggestionsTableView.Hidden = true;
            };
        }
    }
}