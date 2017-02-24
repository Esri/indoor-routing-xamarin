// <copyright file="TableRowSelectedEventArgs.cs" company="Esri, Inc">
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
    using Foundation;

    /// <summary>
    /// Table row selected event arguments class to be used as generic class for all table views.
    /// </summary>
    /// <typeparam name="T">The SelectedItem parameter.</typeparam>
    internal class TableRowSelectedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorRouting.iOS.TableRowSelectedEventArgs`1"/> class.
        /// </summary>
        /// <param name="selectedItem">Selected item.</param>
        /// <param name="selectedItemIndexPath">Selected item index path.</param>
        public TableRowSelectedEventArgs(T selectedItem, NSIndexPath selectedItemIndexPath)
        {
            this.SelectedItem = selectedItem;
            this.SelectedItemLabel = selectedItem.ToString();
            this.SelectedItemIndexPath = selectedItemIndexPath;
        }

        /// <summary>
        /// Gets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        public T SelectedItem { get; }

        /// <summary>
        /// Gets the selected item label.
        /// </summary>
        /// <value>The selected item label.</value>
        public string SelectedItemLabel { get; }

        /// <summary>
        /// Gets the selected item index path.
        /// </summary>
        /// <value>The selected item index path.</value>
        public NSIndexPath SelectedItemIndexPath { get; }
    }
}
