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

using Esri.ArcGISRuntime.Tasks.Geocoding;
using Foundation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models
{
    /// <summary>>
    /// Class handling the source data for the autosuggestions TableView 
    /// </summary>
    internal class AutosuggestionsTableSource : UITableViewSource
    {
        private const string CellIdentifier = "SuggestionCell";

        // Suggestions to display
        private IEnumerable<SuggestResult> _items;

        // Special suggestions corresponding to home location, current device location
        private readonly List<string> _specialSettings = new List<string>();

        // Images corresponding to home location, current device location
        private readonly List<UIImage> _specialSettingsImages = new List<UIImage>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models.AutosuggestionsTableSource"/> class.
        /// </summary>
        /// <param name="items">table items.</param>
        /// <param name="showSpecialItems">Whether to show special home and current location suggestions</param>
        public AutosuggestionsTableSource(IEnumerable<SuggestResult> items, bool showSpecialItems)
        {
            _items = items ?? new List<SuggestResult>();
            ResetSpecialSettings();
            ShouldShowSpecialItems = showSpecialItems;

            // Keep special items section updated when settings change
            AppSettings.CurrentSettings.PropertyChanged += AppSettings_Changed;
        }

        /// <summary>
        /// If true, special suggestions for current location, home location can be shown.
        /// </summary>
        public bool ShouldShowSpecialItems { get; set; }

        /// <summary>
        /// Updates the list of suggestions
        /// </summary>
        public void UpdateSuggestions(IEnumerable<SuggestResult> items) => _items = items ?? new List<SuggestResult>();

        /// <summary>
        /// Occurs when table row is selected.
        /// </summary>
        public event EventHandler<TableRowSelectedEventArgs<string>> TableRowSelected;

        /// <summary>
        /// Updates the special suggestions list for any relevant settings changes
        /// </summary>
        private void AppSettings_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.IsLocationServicesEnabled) ||
                e.PropertyName == nameof(AppSettings.HomeLocation))
            {
                ResetSpecialSettings();
            }
        }

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            try
            {
                if (_specialSettings.Any() && ShouldShowSpecialItems && !_items.Any())
                {
                    return _specialSettings.Count;
                }

                return _items.Count();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Updates the special suggestions list based on settings
        /// </summary>
        private void ResetSpecialSettings()
        {
            _specialSettings.Clear();
            _specialSettingsImages.Clear();

            if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                _specialSettings.Add(AppSettings.LocalizedCurrentLocationString);
                _specialSettingsImages.Add(UIImage.FromBundle("gps-on"));
            }

            if (!string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                _specialSettings.Add(AppSettings.CurrentSettings.HomeLocation);
                _specialSettingsImages.Add(UIImage.FromBundle("home"));
            }
        }

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            // Get an existing cell or create a new one
            var cell = tableView.DequeueReusableCell(CellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier)
            {
                BackgroundColor = tableView.BackgroundColor
            };

            try
            {
                // Either show the special settings item or the regular suggestions depending on settings
                if (_specialSettings.Any() && indexPath.Section == 0 && ShouldShowSpecialItems && !_items.Any())
                {
                    cell.TextLabel.Text = _specialSettings[indexPath.Row];
                    cell.ImageView.Image = _specialSettingsImages[indexPath.Row];
                }
                else
                {
                    var item = _items.ElementAt(indexPath.Row);
                    cell.TextLabel.Text = item.Label;
                    cell.ImageView.Image = null;
                }

                return cell;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// When user selects location from list of autosuggestions, handle selection
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            // Automatically deselect any rows
            tableView.DeselectRow(indexPath, false);

            OnTableRowSelected(indexPath);
        }

        /// <summary>
        /// Override ScrollView behavior to dismiss keyboard on scroll
        /// </summary>
        /// <param name="scrollView">Scroll view.</param>
        public override void Scrolled(UIScrollView scrollView) => scrollView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;

        /// <summary>
        /// Handle the table row selection.
        /// </summary>
        /// <param name="itemIndexPath">Item index path.</param>
        private void OnTableRowSelected(NSIndexPath itemIndexPath)
        {
            try
            {
                if (_specialSettings.Count > 0 && itemIndexPath.Section == 0 && ShouldShowSpecialItems && !_items.Any())
                {
                    var specialItem = _specialSettings[itemIndexPath.Row];
                    TableRowSelected?.Invoke(this, new TableRowSelectedEventArgs<string>(specialItem, itemIndexPath));
                }
                else
                {
                    var item = _items.ElementAt(itemIndexPath.Row);
                    TableRowSelected?.Invoke(this,  new TableRowSelectedEventArgs<string>(item.Label, itemIndexPath));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }
    }
}
