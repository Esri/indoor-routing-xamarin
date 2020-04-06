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
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using UIKit;

    /// <summary>
    /// Controller handles the ui and logic of the user choosing a home location
    /// </summary>
    internal partial class HomeLocationController : UIViewController
    {

        UITableView AutosuggestionsTableView { get; set; }
        UISearchBar HomeLocationSearchBar { get; set; }
        UIButton _clearHomeLocationButton;

        /// <summary>
        /// The home location.
        /// </summary>
        private GeocodeResult homeLocation;

        /// <summary>
        /// The home floor level.
        /// </summary>
        private string floorLevel;

        /// <summary>
        /// Gets the coordinates for the home location
        /// </summary>
        public GeocodeResult HomeLocation
        {
            get
            {
                return this.homeLocation;
            }

            private set
            {
                if (this.homeLocation != value && value != null)
                {
                    this.homeLocation = value;

                    // Save extent of home location and floor level to Settings file
                    CoordinatesKeyValuePair<string, double>[] homeCoordinates =
                    {
                    new CoordinatesKeyValuePair<string, double>("X", this.homeLocation.DisplayLocation.X),
                    new CoordinatesKeyValuePair<string, double>("Y", this.homeLocation.DisplayLocation.Y),
                    new CoordinatesKeyValuePair<string, double>("WKID", this.homeLocation.DisplayLocation.SpatialReference.Wkid)
                    };

                    AppSettings.CurrentSettings.HomeCoordinates = homeCoordinates;

                    // Save user settings
                    Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
                }
            }
        }

        /// <summary>
        /// Gets or sets the floor level for the home location.
        /// </summary>
        /// <value>The floor level.</value>
        public string FloorLevel
        {
            get
            {
                return this.floorLevel;
            }

            set
            {
                if (this.floorLevel != value && value != string.Empty)
                {
                    this.floorLevel = value;
                    AppSettings.CurrentSettings.HomeFloorLevel = this.floorLevel;

                    // Save user settings
                    Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
                }
            }
        }

        

        public override void LoadView()
        {
            base.LoadView();
            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            HomeLocationSearchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            HomeLocationSearchBar.Placeholder = "Search for a location";
            HomeLocationSearchBar.BackgroundImage = new UIImage();
            AutosuggestionsTableView = new UITableView { TranslatesAutoresizingMaskIntoConstraints = false };
            AutosuggestionsTableView.BackgroundColor = UIColor.Clear;

            _clearHomeLocationButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _clearHomeLocationButton.SetTitle("Clear Home Location", UIControlState.Normal);

            View.AddSubviews(HomeLocationSearchBar, AutosuggestionsTableView, _clearHomeLocationButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                HomeLocationSearchBar.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                HomeLocationSearchBar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                HomeLocationSearchBar.TrailingAnchor.ConstraintEqualTo(_clearHomeLocationButton.LeadingAnchor, -8),
                _clearHomeLocationButton.CenterYAnchor.ConstraintEqualTo(HomeLocationSearchBar.CenterYAnchor),
                _clearHomeLocationButton.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -8),
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

            _clearHomeLocationButton.TouchUpInside -= _clearHomeLocationButton_TouchUpInside;

            HomeLocationSearchBar.TextChanged -= HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked -= HomeLocationSearchBar_SearchButtonClicked;
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;

            _clearHomeLocationButton.TouchUpInside += _clearHomeLocationButton_TouchUpInside;

            HomeLocationSearchBar.TextChanged += HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked += HomeLocationSearchBar_SearchButtonClicked;

            base.ViewWillAppear(animated);
        }

        private async void HomeLocationSearchBar_SearchButtonClicked(object sender, EventArgs e)
        {
            var locationText = ((UISearchBar)sender).Text;
            await SetHomeLocationAsync(locationText);
        }

        private async void HomeLocationSearchBar_TextChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            // This is the method that is called when the user searchess
            await GetSuggestionsFromLocatorAsync();
        }

        private void _clearHomeLocationButton_TouchUpInside(object sender, EventArgs e)
        {
            // TODO clear this up
            AppSettings.CurrentSettings.HomeLocation = "";
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));

        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
        /// </summary>
        /// <returns>Async task</returns>
        private async Task GetSuggestionsFromLocatorAsync()
        {
            var suggestions = await LocationViewModel.Instance.GetLocationSuggestionsAsync(this.HomeLocationSearchBar.Text);
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
        private async void TableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            var selectedItem = e.SelectedItem;
            if (selectedItem != null)
            {
                this.HomeLocationSearchBar.Text = selectedItem;
                await this.SetHomeLocationAsync(selectedItem);
            }
        }

        /// <summary>
        /// Sets the home location for the user and saves it into settings.
        /// </summary>
        /// <param name="locationText">Location text.</param>
        /// <returns>Async task</returns>
        private async Task SetHomeLocationAsync(string locationText)
        {
            AppSettings.CurrentSettings.HomeLocation = locationText;
            this.HomeLocation = await LocationViewModel.Instance.GetSearchedLocationAsync(locationText);
            this.FloorLevel = await LocationViewModel.Instance.GetFloorLevelFromQueryAsync(locationText);

            NavigationController.PopViewController(true);
        }
    }
}