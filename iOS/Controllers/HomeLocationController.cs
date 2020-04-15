// <copyright file="HomeLocationController.cs" company="Esri, Inc">
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
    using System.IO;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using UIKit;

    /// <summary>
    /// Controller handles the ui and logic of the user choosing a home location
    /// </summary>
    internal partial class HomeLocationController : UIViewController
    {
        private MapViewModel _viewModel;

        UITableView AutosuggestionsTableView { get; set; }
        UISearchBar HomeLocationSearchBar { get; set; }

        public HomeLocationController(MapViewModel viewModel) : base()
        {
            _viewModel = viewModel;
        }

        public override void LoadView()
        {
            base.LoadView();
            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            HomeLocationSearchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            HomeLocationSearchBar.Placeholder = "LocationSearchBarPlaceholder".AsLocalized();
            HomeLocationSearchBar.BackgroundImage = new UIImage();
            HomeLocationSearchBar.ShowsCancelButton = true;
            HomeLocationSearchBar.Text = AppSettings.CurrentSettings.HomeLocation;
            AutosuggestionsTableView = new UITableView { TranslatesAutoresizingMaskIntoConstraints = false };
            AutosuggestionsTableView.BackgroundColor = UIColor.Clear;

            View.AddSubviews(HomeLocationSearchBar, AutosuggestionsTableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                HomeLocationSearchBar.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                HomeLocationSearchBar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                HomeLocationSearchBar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                AutosuggestionsTableView.TopAnchor.ConstraintEqualTo(HomeLocationSearchBar.BottomAnchor),
                AutosuggestionsTableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                AutosuggestionsTableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                AutosuggestionsTableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });
        }

        /// <summary>
        /// Overrides the behavior of the controller once the view has loaded
        /// </summary>
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            this.HomeLocationSearchBar.BecomeFirstResponder();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            HomeLocationSearchBar.TextChanged -= HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked -= HomeLocationSearchBar_SearchButtonClicked;

            HomeLocationSearchBar.CancelButtonClicked -= _clearHomeLocationButton_TouchUpInside;
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;

            HomeLocationSearchBar.CancelButtonClicked += _clearHomeLocationButton_TouchUpInside;

            HomeLocationSearchBar.TextChanged += HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked += HomeLocationSearchBar_SearchButtonClicked;

            base.ViewWillAppear(animated);
        }

        private void HomeLocationSearchBar_SearchButtonClicked(object sender, EventArgs e)
        {
            var locationText = ((UISearchBar)sender).Text;
            SetHomeLocationAsync(locationText);
        }

        private async void HomeLocationSearchBar_TextChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            try
            {
                // This is the method that is called when the user searchess
                await GetSuggestionsFromLocatorAsync();
            }
            catch (Exception)
            {
                // TODO - log exception
            }
        }

        private void _clearHomeLocationButton_TouchUpInside(object sender, EventArgs e)
        {
            this.HomeLocationSearchBar.Text = "";
            this.SetHomeLocationAsync("");

        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
        /// </summary>
        /// <returns>Async task</returns>
        private async Task GetSuggestionsFromLocatorAsync()
        {
            var suggestions = await _viewModel.GetLocationSuggestionsAsync(this.HomeLocationSearchBar.Text);
            if (suggestions == null || suggestions.Count == 0)
            {
                this.AutosuggestionsTableView.Hidden = true;
            }

            // Only show the floors tableview if the buildings in view have more than one floor
            if (suggestions.Count > 0)
            {
                // Show the tableview with autosuggestions and populate it
                this.AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions, false);
                tableSource.TableRowSelected += this.TableSource_TableRowSelected;
                this.AutosuggestionsTableView.Source = tableSource;
                this.AutosuggestionsTableView.ReloadData();
            }
        }

        /// <summary>
        /// Get the value selected in the Autosuggestions Table
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event args.</param>
        private void TableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            var selectedItem = e.SelectedItem;
            if (selectedItem != null)
            {
                HomeLocationSearchBar.Text = selectedItem;
                SetHomeLocationAsync(selectedItem);
            }
        }

        /// <summary>
        /// Sets the home location for the user and saves it into settings.
        /// </summary>
        /// <param name="locationText">Location text.</param>
        /// <returns>Async task</returns>
        private async void SetHomeLocationAsync(string locationText)
        {
            try
            {
                var homeLocation = await _viewModel.GetSearchedLocationAsync(locationText);
                //var floorLevel = await _viewModel.GetFloorLevelFromQueryAsync(locationText);
                // TODO - re-enable saving the floor level

                AppSettings.CurrentSettings.HomeLocation = locationText;
                AppSettings.CurrentSettings.HomeCoordinates = new CoordinatesKeyValuePair<string, double>[]
                    {
                    new CoordinatesKeyValuePair<string, double>("X", homeLocation.DisplayLocation.X),
                    new CoordinatesKeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
                    new CoordinatesKeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid)
                    };
                //AppSettings.CurrentSettings.HomeFloorLevel = floorLevel; // TODO - re-enable saving the floor level

                _ = Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
            }
            catch (Exception)
            {
                // TODO - log error
            }
            
            NavigationController.PopViewController(true);
        }
    }
}