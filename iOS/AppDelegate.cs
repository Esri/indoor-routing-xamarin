using System;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		// will hold the value of the finite background task
		nint taskID = -1;
		string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		public override UIWindow Window
		{
			get;
			set;
		}

		/// <summary>
		/// Overrides the behavior of the application when it has finished launching
		/// </summary>
		/// <param name="application">Application.</param>
		public override async void FinishedLaunching(UIApplication application)
		{
			
		}

		/// <summary>
		/// Overrides the behavior of the application when it has entered background mode
		/// </summary>
		/// <param name="application">Application.</param>
		public override async void DidEnterBackground(UIApplication application)
		{
			// Begin Finite-Length Task.
			taskID = UIApplication.SharedApplication.BeginBackgroundTask(null);

			// Start saving the user choices.
			await Task.Run(() => AppSettings.SaveSettings(Path.Combine(settingsPath, "AppSettings.xml")));

			// End Finite-Length Task, finished.
			if (taskID != -1)
			{
				UIApplication.SharedApplication.EndBackgroundTask(taskID);
				taskID = -1;
			}
		}

		/// <summary>
		/// Call this when the transfer of all files is done.
		/// </summary>
		public static Action BackgroundSessionCompletionHandler;
	}
}

