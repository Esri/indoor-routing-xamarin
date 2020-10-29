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

using System;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    /// <summary>
    /// Shows a floor picker
    /// </summary>
    public sealed class FloorsTableView : SelfSizedTableView
    {
        private readonly MapViewModel _viewModel;

        public FloorsTableView(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            Layer.CornerRadius = ApplicationTheme.CornerRadius;
            SeparatorColor = ApplicationTheme.SeparatorColor;
            BackgroundColor = UIColor.Clear;
            BackgroundView = new UIVisualEffectView(ApplicationTheme.PanelBackgroundMaterial);

            Source = new FloorsTableSource(viewModel);

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Updates the floor list & selection when viewmodel properties change
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.SelectedFloorLevel) ||
                e.PropertyName == nameof(_viewModel.CurrentVisibleFloors))
            {
                BeginInvokeOnMainThread(() =>
                {
                    ReloadData();
                    UpdateSelectedFloor();

                    if (_viewModel.CurrentVisibleFloors != null)
                    {
                        Hidden = _viewModel.CurrentVisibleFloors?.Count() < 2;
                    }
                    else
                    {
                        Hidden = true;
                    }
                });
            }
                
        }

        /// <summary>
        /// Updates the selected row based on the selected floor from the viewmodel
        /// </summary>
        private void UpdateSelectedFloor()
        {
            if (_viewModel.SelectedFloorLevel != null && _viewModel.CurrentVisibleFloors != null)
            {
                var selectedFloorNsIndex = GetTableViewRowIndex(_viewModel.SelectedFloorLevel, _viewModel.CurrentVisibleFloors.ToArray(), 0);
                if (selectedFloorNsIndex != null)
                {
                    SelectRow(selectedFloorNsIndex, false, UITableViewScrollPosition.None);
                }
            }
        }

        /// <summary>
        /// Gets the index of the table view row matching rowValue.
        /// </summary>
        /// <returns>The table view row index.</returns>
        /// <param name="rowValue">Row value.</param>
        /// <param name="tableSource">Table source.</param>
        /// <param name="section">TableView Section.</param>
        private NSIndexPath GetTableViewRowIndex(string rowValue, string[] tableSource, nint section)
        {
            var rowIndex = tableSource.Select((rowItem, index) => new { rowItem, index }).FirstOrDefault(i => i.rowItem == rowValue)?.index;
            if (rowIndex.HasValue)
            {
                return NSIndexPath.FromRowSection(rowIndex.Value, section);
            }
            return null;
        }
    }
}
