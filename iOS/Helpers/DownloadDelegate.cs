using System;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Download delegate handles logic of the background download.
	/// </summary>
	class DownloadDelegate : NSUrlSessionDownloadDelegate
	{
		internal DownloadDelegate(DownloadController controller) : base()
		{
			this.controller = controller;
		}

		/// <summary>
		/// Reference to the download controller 
		/// </summary>
		readonly DownloadController controller;

		/// <summary>
		/// Gets called as data is being received
		/// </summary>
		/// <param name="session">Session.</param>
		/// <param name="downloadTask">Download task.</param>
		/// <param name="bytesWritten">Bytes written.</param>
		/// <param name="totalBytesWritten">Total bytes written.</param>
		/// <param name="totalBytesExpectedToWrite">Total bytes expected to write.</param>
		public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
		{
			var localIdentifier = downloadTask.TaskIdentifier;
			var percentage = (float)totalBytesWritten / (float)totalBytesExpectedToWrite;

			InvokeOnMainThread(() => controller.UpdateProgress(percentage));
		}

		/// <summary>
		/// Gets called when the download has been completed.
		/// </summary>
		/// <param name="session">Session.</param>
		/// <param name="downloadTask">Download task.</param>
		/// <param name="location">Location.</param>
		public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
		{
			// The download location is the location of the file
			var sourceFile = location.Path;

			// Copy over to documents folder. Note that we must use NSFileManager here! File.Copy() will not be able to access the source location.
			var fileManager = NSFileManager.DefaultManager;

			// Remove any existing files in our destination
			NSError error;
			fileManager.Remove(DownloadController.targetFilename, out error);
			var success = fileManager.Copy(sourceFile, DownloadController.targetFilename, out error);
			if (!success)
			{
				Console.WriteLine("Error during the copy: {0}", error.LocalizedDescription);
			}

			InvokeOnMainThread(() => this.controller.LoadMapView());

		}

		/// <summary>
		/// Gets called when a download is done. Does not necessarily indicate an error
		/// unless the NSError parameter is not null.
		/// </summary>
		public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
		{
			if (error == null)
			{
				return;
			}

			// If error indeed occured, cancel the task
			task.Cancel();
		}

		/// <summary>
		/// Gets called by iOS if all pending transfers are done. This will only be called if the app was backgrounded.
		/// </summary>
		public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
		{
			// Nothing more to be done. This is the place where we have to call the completion handler we get passed in in AppDelegate.
			var handler = AppDelegate.BackgroundSessionCompletionHandler;
			AppDelegate.BackgroundSessionCompletionHandler = null;
			if (handler != null)
			{
				controller.BeginInvokeOnMainThread(() =>
				{
					// Bring up a local notification to take the user back to our app.
					var notif = new UILocalNotification
					{
						AlertBody = "Indoor Navigation: Mobile Map Package has been downloaded successfully."
					};
					UIApplication.SharedApplication.PresentLocalNotificationNow(notif);

					// Invoke the completion handler. This will tell iOS to update the snapshot in the task manager.
					handler.Invoke();
				});
			}
		}
	}
}
