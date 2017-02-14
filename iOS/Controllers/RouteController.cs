// <copyright file="RouteController.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
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
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.RouteController"/> class.
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
                EndSearchBar.Text = this.EndLocation;
            }

            // Set start location as the current location, if available
            if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                this.StartLocation = "Current Location";
                StartSearchBar.Text = this.StartLocation;
            }
            // Set start location as home location if available
            else if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                this.StartLocation = AppSettings.CurrentSettings.HomeLocation;
                StartSearchBar.Text = this.StartLocation;
            }


            // Set text changed event on the start search bar
            StartSearchBar.TextChanged += async (sender, e) =>
            {
                // This is the method that is called when the user searchess
                await GetSuggestionsFromLocatorAsync(((UISearchBar)sender).Text, true);
            };

            StartSearchBar.SearchButtonClicked += (sender, e) =>
            {
                if (this.StartLocation != "Current Location")
                {
                     StartLocation = ((UISearchBar)sender).Text;
                    AutosuggestionsTableView.Hidden = true;
                }
            };

            // Set text changed event on the end search bar
            EndSearchBar.TextChanged += async (sender, e) =>
            {
                // This is the method that is called when the user searches
                await GetSuggestionsFromLocatorAsync(((UISearchBar)sender).Text, false);
            };

            EndSearchBar.SearchButtonClicked += (sender, e) =>
            {
                EndLocation = ((UISearchBar)sender).Text;
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

            if (segue.Identifier == "BackFromRouteSegue")
            {
                var mapViewController = segue.DestinationViewController as MapViewController;

                // Geocode the locations selected by the user
                try
                {
                    if (this.StartLocation != "Current Location")
                    {
                        var fromLocationFeature = await LocationViewModel.LocationViewModelInstance.GetRoomFeatureAsync(this.StartLocation);
                        var toLocationFeature = await LocationViewModel.LocationViewModelInstance.GetRoomFeatureAsync(this.EndLocation);

                        var fromLocationPoint = fromLocationFeature.Geometry.Extent.GetCenter();
                        var toLocationPoint = toLocationFeature.Geometry.Extent.GetCenter();

                        var route = await LocationViewModel.LocationViewModelInstance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);
                        mapViewController.FromLocationFeature = fromLocationFeature;
                        mapViewController.ToLocationFeature = toLocationFeature;

                        mapViewController.Route = route;
                    }
                    else
                    {
                        var toLocationFeature = await LocationViewModel.LocationViewModelInstance.GetRoomFeatureAsync(this.EndLocation);

                        var fromLocationPoint = LocationViewModel.LocationViewModelInstance.CurrentLocation;
                        var toLocationPoint = toLocationFeature.Geometry.Extent.GetCenter();

                        var route = await LocationViewModel.LocationViewModelInstance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                        mapViewController.ToLocationFeature = toLocationFeature;

                        mapViewController.Route = route;
                    }
                }
                catch
                {
                    mapViewController.Route = null;
                }
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
            var suggestions = await LocationViewModel.LocationViewModelInstance.GetLocationSuggestionsAsync(searchText);

            if (suggestions == null || suggestions.Count == 0)
            {
                AutosuggestionsTableView.Hidden = true;
            }

            // Only show the floors tableview if the buildings in view have more than one floor
            if (suggestions.Count > 0)
            {
                // Show the tableview with autosuggestions and populate it
                AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions);
                tableSource.TableRowSelected += this.TableSource_TableRowSelected;
                AutosuggestionsTableView.Source = tableSource;

                AutosuggestionsTableView.ReloadData();

                // Auto extend or shrink the tableview based on the content inside
                var frame = AutosuggestionsTableView.Frame;
                frame.Height = AutosuggestionsTableView.ContentSize.Height;
                AutosuggestionsTableView.Frame = frame;
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
                StartSearchBar.Text = selectedItem.Label;
                this.StartLocation = selectedItem.Label;
                StartSearchBar.ResignFirstResponder();
            }
            else
            {
                EndSearchBar.Text = selectedItem.Label;
                this.EndLocation = selectedItem.Label;
                EndSearchBar.ResignFirstResponder();
            }

            // Dismiss autosuggest table and keyboard
            AutosuggestionsTableView.Hidden = true;
        }
    }
}