// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Controller handles the ui and logic of the user choosing a home location
    /// </summary>
    internal class HomeLocationController : UIViewController
    {
        private readonly MapViewModel _viewModel;

        private UITableView AutosuggestionsTableView { get; set; }
        private UISearchBar HomeLocationSearchBar { get; set; }

        public HomeLocationController(MapViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public override void LoadView()
        {
            base.LoadView();
            View = new UIView
            {
                BackgroundColor = ApplicationTheme.BackgroundColor, TintColor = ApplicationTheme.ActionBackgroundColor
            };

            HomeLocationSearchBar = new UISearchBar
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Placeholder = "LocationSearchBarPlaceholder".Localize(),
                BackgroundImage = new UIImage(),
                ShowsCancelButton = true,
                Text = AppSettings.CurrentSettings.HomeLocation
            };
            AutosuggestionsTableView = new UITableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false, BackgroundColor = UIColor.Clear
            };

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
            HomeLocationSearchBar.BecomeFirstResponder();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            HomeLocationSearchBar.TextChanged -= HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked -= HomeLocationSearchBar_SearchButtonClicked;

            HomeLocationSearchBar.CancelButtonClicked -= ClearHome_Clicked;
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;

            HomeLocationSearchBar.CancelButtonClicked += ClearHome_Clicked;

            HomeLocationSearchBar.TextChanged += HomeLocationSearchBar_TextChanged;

            HomeLocationSearchBar.SearchButtonClicked += HomeLocationSearchBar_SearchButtonClicked;

            base.ViewWillAppear(animated);
        }

        private void HomeLocationSearchBar_SearchButtonClicked(object sender, EventArgs e)
        {
            var locationText = ((UISearchBar) sender).Text;
            SetHomeLocationAsync(locationText);
        }

        private async void HomeLocationSearchBar_TextChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            try
            {
                // This is the method that is called when the user searches
                await GetSuggestionsFromLocatorAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        private void ClearHome_Clicked(object sender, EventArgs e)
        {
            HomeLocationSearchBar.Text = "";
            SetHomeLocationAsync("");
        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the search field.
        /// </summary>
        /// <returns>Async task</returns>
        private async Task GetSuggestionsFromLocatorAsync()
        {
            var suggestions = await _viewModel.GetLocationSuggestionsAsync(HomeLocationSearchBar.Text);
            if (suggestions == null || suggestions.Count == 0)
            {
                AutosuggestionsTableView.Hidden = true;
            }

            // Only show the floors tableview if the buildings in view have more than one floor
            if (suggestions?.Any() ?? false)
            {
                // Show the tableview with autosuggestions and populate it
                AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions, false);
                tableSource.TableRowSelected += TableSource_TableRowSelected;
                AutosuggestionsTableView.Source = tableSource;
                AutosuggestionsTableView.ReloadData();
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
                GeocodeResult homeLocation = await _viewModel.GetSearchedLocationAsync(locationText);
                Feature homeFeature = await _viewModel.GetRoomFeatureAsync(locationText);
                if (homeFeature != null)
                {
                    AppSettings.CurrentSettings.HomeCoordinates = new[]
                    {
                        new KeyValuePair<string, double>("X", homeLocation.DisplayLocation.X),
                        new KeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
                        new KeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid)
                    };
                    AppSettings.CurrentSettings.HomeFloorLevel = homeFeature
                        .Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString();
                    AppSettings.CurrentSettings.HomeLocation = locationText;
                }
                else
                {
                    AppSettings.CurrentSettings.HomeCoordinates = null;
                    AppSettings.CurrentSettings.HomeFloorLevel = null;
                    AppSettings.CurrentSettings.HomeLocation = null;
                }

                await Task.Run(() =>
                    AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
                _viewModel.UpdateHomeLocation();
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }

            NavigationController.PopViewController(true);
        }
    }
}