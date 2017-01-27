// <copyright file="TableRowSelectedEventArgs.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation.iOS
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
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.TableRowSelectedEventArgs`1"/> class.
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
