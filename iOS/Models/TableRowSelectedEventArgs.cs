using System;
using Foundation;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Table row selected event arguments class to be used as generic class for all table views.
	/// </summary>
	internal class TableRowSelectedEventArgs<T> : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.TableRowSelectedEventArgs`1"/> class.
		/// </summary>
		/// <param name="selectedItem">Selected item.</param>
		/// <param name="selectedItemIndexPath">Selected item index path.</param>
		public TableRowSelectedEventArgs(T selectedItem, NSIndexPath selectedItemIndexPath)
		{
			SelectedItem = selectedItem;
			SelectedItemLabel = selectedItem.ToString();
			SelectedItemIndexPath = selectedItemIndexPath;
		}

		public T SelectedItem { get; }
		public string SelectedItemLabel { get; }
		public NSIndexPath SelectedItemIndexPath { get; }
	}
}
