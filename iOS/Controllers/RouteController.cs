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
namespace IndoorRouting.iOS
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
        /// Initializes a new instance of the <see cref="T:IndoorRouting.iOS.RouteController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        public RouteController(IntPtr handle) : base(handle)
        {
        }

        /// <summary>
        /// Gets or sets the end location.
        /// </summary>
        /// <value>The end location.</value>
        public string EndLocation { get; set; }

        /// <summary>
        /// Gets or sets the start location.
        /// </summary>
        /// <value>The start location.</value>
        public string StartLocation { get; set; }

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

            if (this.EndLocation != null)
            {
                this.EndSearchBar.Text = this.EndLocation;
            }

            // Set start location as the current location, if available
            // Set start location as home location if available
            if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                this.StartLocation = "Current Location";
                this.StartSearchBar.Text = this.StartLocation;
            }
            else if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                this.StartLocation = AppSettings.CurrentSettings.HomeLocation;
                this.StartSearchBar.Text = this.StartLocation;
            }

            // Set text changed event on the start search bar
            this.StartSearchBar.TextChanged += async (sender, e) =>
            {
                // This is the method that is called when the user searchess
                StartLocation = ((UISearchBar)sender).Text;
                await GetSuggestionsFromLocatorAsync(((UISearchBar)sender).Text, true);
            };

            this.StartSearchBar.SearchButtonClicked += (sender, e) =>
            {
                if (this.StartLocation != "Current Location")
                {
                    AutosuggestionsTableView.Hidden = true;
                }
            };

            // Set text changed event on the end search bar
            this.EndSearchBar.TextChanged += async (sender, e) =>
            {
                // This is the method that is called when the user searches
                EndLocation = ((UISearchBar)sender).Text;
                await GetSuggestionsFromLocatorAsync(((UISearchBar)sender).Text, false);
            };

            this.EndSearchBar.SearchButtonClicked += (sender, e) =>
            {
                AutosuggestionsTableView.Hidden = true;
            };
        }

        /// <summary>
        /// Prepares for segueto go back to the Main View Controller. This segue is initiated by the Route button
        /// </summary>
        /// <param name="segue">Segue control.</param>
        /// <param name="sender">Sender control.</param>
        public async override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);

            var mapViewController = segue.DestinationViewController as MapViewController;

            // Geocode the locations selected by the user
            try
            {
                if (this.StartLocation != "Current Location")
                {
                    var fromLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(this.StartLocation);
                    var toLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(this.EndLocation);

                    var fromLocationPoint = fromLocationFeature.Geometry.Extent.GetCenter();
                    var toLocationPoint = toLocationFeature.Geometry.Extent.GetCenter();

                    var route = await LocationViewModel.Instance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);
                    mapViewController.FromLocationFeature = fromLocationFeature;
                    mapViewController.ToLocationFeature = toLocationFeature;

                    mapViewController.Route = route;
                }
                else
                {
                    var toLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(this.EndLocation);

                    var fromLocationPoint = LocationViewModel.Instance.CurrentLocation;
                    var toLocationPoint = toLocationFeature.Geometry.Extent.GetCenter();

                    var route = await LocationViewModel.Instance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                    mapViewController.ToLocationFeature = toLocationFeature;

                    mapViewController.Route = route;
                }
            }
            catch
            {
                mapViewController.Route = null;
            }
        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
        /// </summary>
        /// <returns>The suggestions from locator async.</returns>
        /// <param name="searchText">Search text.</param>
        /// <param name="startSearchBarFlag">If set to <c>true</c> start search bar flag.</param>
        private async Task GetSuggestionsFromLocatorAsync(string searchText, bool startSearchBarFlag)
        {
            this.startSearchBarFlag = startSearchBarFlag;
            var suggestions = await LocationViewModel.Instance.GetLocationSuggestionsAsync(searchText);

            if (suggestions == null || suggestions.Count == 0)
            {
                this.AutosuggestionsTableView.Hidden = true;
            }

            // Only show the floors tableview if the buildings in view have more than one floor
            if (suggestions.Count > 0)
            {
                // Show the tableview with autosuggestions and populate it
                this.AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions);
                tableSource.TableRowSelected += this.TableSource_TableRowSelected;
                this.AutosuggestionsTableView.Source = tableSource;

                this.AutosuggestionsTableView.ReloadData();

                // Auto extend or shrink the tableview based on the content inside
                var frame = this.AutosuggestionsTableView.Frame;
                frame.Height = this.AutosuggestionsTableView.ContentSize.Height;
                this.AutosuggestionsTableView.Frame = frame;
            }
        }

        /// <summary>
        /// Get the value selected in the Autosuggestions Table
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        private void TableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<SuggestResult> e)
        {
            var selectedItem = e.SelectedItem;

            // Test which search box the initial request came from
            if (this.startSearchBarFlag == true)
            {
                this.StartSearchBar.Text = selectedItem.Label;
                this.StartLocation = selectedItem.Label;
                this.StartSearchBar.ResignFirstResponder();
            }
            else
            {
                this.EndSearchBar.Text = selectedItem.Label;
                this.EndLocation = selectedItem.Label;
                this.EndSearchBar.ResignFirstResponder();
            }

            // Dismiss autosuggest table and keyboard
            this.AutosuggestionsTableView.Hidden = true;
        }
    }
}