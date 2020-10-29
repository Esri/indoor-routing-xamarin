// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// https://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Controller handles the UI for choosing a new home location
    /// </summary>
    internal class HomeLocationController : UIViewController
    {
        private readonly MapViewModel _viewModel;
        private UITableView _autosuggestionsTableView;
        private UISearchBar _homeLocationSearchBar;
        private AutosuggestionsTableSource _suggestionSource;

        public HomeLocationController(MapViewModel viewModel)
        {
            Title = "ChooseHomeLocationTitle".Localize();
            _viewModel = viewModel;
        }

        public override void LoadView()
        {
            // Create views
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor };

            _homeLocationSearchBar = new UISearchBar
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Placeholder = "LocationSearchBarPlaceholder".Localize(),
                BackgroundImage = new UIImage(),
                ShowsCancelButton = true,
                Text = AppSettings.CurrentSettings.HomeLocation,
                TintColor = ApplicationTheme.ActionBackgroundColor
            };

            _autosuggestionsTableView = new UITableView { TranslatesAutoresizingMaskIntoConstraints = false, BackgroundColor = UIColor.Clear };

            // Add views
            View.AddSubviews(_homeLocationSearchBar, _autosuggestionsTableView);

            // Lay out views
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _homeLocationSearchBar.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _homeLocationSearchBar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _homeLocationSearchBar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _autosuggestionsTableView.TopAnchor.ConstraintEqualTo(_homeLocationSearchBar.BottomAnchor),
                _autosuggestionsTableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _autosuggestionsTableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _autosuggestionsTableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });
        }

        /// <summary>
        /// Overrides the behavior of the controller once the view has loaded
        /// </summary>
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // Automatically put the cursor in the search bar and show the keyboard
            _homeLocationSearchBar.BecomeFirstResponder();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            _homeLocationSearchBar.TextChanged -= HomeLocationSearchBar_TextChanged;
            _homeLocationSearchBar.SearchButtonClicked -= HomeLocationSearchBar_SearchButtonClicked;
            _homeLocationSearchBar.CancelButtonClicked -= ClearHome_Clicked;
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar if applicable
            if (NavigationController != null)
            {
                NavigationController.NavigationBarHidden = false;
                NavigationController.NavigationBar.TintColor = ApplicationTheme.AccessoryButtonColor;
            }

            _homeLocationSearchBar.CancelButtonClicked += ClearHome_Clicked;
            _homeLocationSearchBar.TextChanged += HomeLocationSearchBar_TextChanged;
            _homeLocationSearchBar.SearchButtonClicked += HomeLocationSearchBar_SearchButtonClicked;

            base.ViewWillAppear(animated);
        }

        /// <summary>
        /// Sets the home location with the user's search
        /// </summary>
        private void HomeLocationSearchBar_SearchButtonClicked(object sender, EventArgs e) => SetHomeLocationAsync(_homeLocationSearchBar.Text);

        /// <summary>
        /// Updates suggestions as the user types
        /// </summary>
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


        /// <summary>
        /// Clear the home location when user cancels the search
        /// </summary>
        private void ClearHome_Clicked(object sender, EventArgs e)
        {
            _homeLocationSearchBar.Text = null;
            SetHomeLocationAsync(null);
        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the search field.
        /// </summary>
        private async Task GetSuggestionsFromLocatorAsync()
        {
            // Get the suggestions
            var suggestions = await _viewModel.GetLocationSuggestionsAsync(_homeLocationSearchBar.Text);

            // Only show the floors tableview if the buildings in view have more than one floor
            if (suggestions?.Any() ?? false)
            {
                // Show the tableview with autosuggestions and populate it
                _autosuggestionsTableView.Hidden = false;

                // Unsubscribe from events to prevent memory leak
                if (_suggestionSource != null)
                {
                    _suggestionSource.TableRowSelected -= TableSource_TableRowSelected;
                }

                // Create new table source and use it
                _suggestionSource = new AutosuggestionsTableSource(suggestions, false);
                _suggestionSource.TableRowSelected += TableSource_TableRowSelected;
                _autosuggestionsTableView.Source = _suggestionSource;
                _autosuggestionsTableView.ReloadData();
            }
            else
            {
                _autosuggestionsTableView.Hidden = true;
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
                _homeLocationSearchBar.Text = selectedItem;
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
                        new SerializableKeyValuePair<string, double>("X", homeLocation.DisplayLocation.X),
                        new SerializableKeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
                        new SerializableKeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid)
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