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
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models
{
    /// <summary>>
    /// Class handling the source data for the autosuggestions TableView 
    /// </summary>
    internal class AutosuggestionsTableSource : UITableViewSource
    {
        /// <summary>
        /// The items int he table.
        /// </summary>
        private IEnumerable<SuggestResult> _items;

        private readonly List<string> _specialSettings = new List<string>();

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string _cellIdentifier;

        public bool ShouldShowSpecialItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models.AutosuggestionsTableSource"/> class.
        /// </summary>
        /// <param name="items">table items.</param>
        /// <param name="showSpecialItems">Whether to show special home and current location suggestions</param>
        internal AutosuggestionsTableSource(IEnumerable<SuggestResult> items, bool showSpecialItems)
        {
            _items = items ?? new List<SuggestResult>();
            _cellIdentifier = "cell_id";
            ResetSpecialSettings();
            ShouldShowSpecialItems = showSpecialItems;
        }

        public void UpdateSuggestions(IEnumerable<SuggestResult> items)
        {
            _items = items ?? new List<SuggestResult>();
        }

        /// <summary>
        /// Occurs when table row is selected.
        /// </summary>
        public event EventHandler<TableRowSelectedEventArgs<string>> TableRowSelected;

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
                else
                {
                    return _items.Count();
                }
            }
            catch
            {
                return 0;
            }
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        private void ResetSpecialSettings()
        {
            _specialSettings.Clear();
            if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                _specialSettings.Add("CurrentLocationLabel".Localize());
            }

            if (!String.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                _specialSettings.Add(AppSettings.CurrentSettings.HomeLocation);
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
            var cell = tableView.DequeueReusableCell(_cellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, _cellIdentifier)
            {
                BackgroundColor = tableView.BackgroundColor
            };

            try
            {
                if (_specialSettings.Any() && indexPath.Section == 0 && ShouldShowSpecialItems && !_items.Any())
                {
                    cell.TextLabel.Text = _specialSettings[indexPath.Row];
                }
                else
                {
                    var item = _items.ElementAt(indexPath.Row);
                    cell.TextLabel.Text = item.Label;
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
            OnTableRowSelected(indexPath);
            tableView.DeselectRow(indexPath, false);
        }

        /// <summary>
        /// Override ScrollView behavior to dismiss keyboard on scroll
        /// </summary>
        /// <param name="scrollView">Scroll view.</param>
        public override void Scrolled(UIScrollView scrollView)
        {
            scrollView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
        }

        /// <summary>
        /// Handle the table row selection.
        /// </summary>
        /// <param name="itemIndexPath">Item index path.</param>
        private void OnTableRowSelected(NSIndexPath itemIndexPath)
        {
            try
            {
                if (_specialSettings.Count > 0 && itemIndexPath.Section == 0 && ShouldShowSpecialItems &&
                    !_items.Any())
                {
                    var specialItem = _specialSettings[itemIndexPath.Row];
                    TableRowSelected?.Invoke(this,
                        new TableRowSelectedEventArgs<string>(specialItem, itemIndexPath));
                }
                else
                {
                    var item = _items.ElementAt(itemIndexPath.Row);
                    TableRowSelected?.Invoke(this,
                        new TableRowSelectedEventArgs<string>(item.Label, itemIndexPath));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }
    }
}
