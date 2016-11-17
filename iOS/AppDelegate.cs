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
	public class AppDelegate : UIApplicationDelegate
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

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			// Override point for customization after application launch.
			// If not required for your application you can safely delete this method

			// When the application has finished loading, bring in the settings

			GlobalSettings.currentSettings = AppSettings.LoadAppSettings(Path.Combine(settingsPath, "AppSettings.xml")).Result;

			return true;
		}

		public override void OnResignActivation(UIApplication application)
		{
			// Invoked when the application is about to move from active to inactive state.
			// This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
			// or when the user quits the application and it begins the transition to the background state.
			// Games should use this method to pause the game.
		}

		public override async void DidEnterBackground(UIApplication application)
		{
			// Use this method to release shared resources, save user data, invalidate timers and store the application state.
			// If your application supports background exection this method is called instead of WillTerminate when the user quits.

			// Begin Finite-Length Task.
			taskID = UIApplication.SharedApplication.BeginBackgroundTask(null);

			// Start saving the user choices.
			await Task.Run(() => AppSettings.SaveSettings(Path.Combine(settingsPath, "AppSettings.xml")));

			// End Finite-Length Task, finished.
			if (this.taskID != -1)
			{
				UIApplication.SharedApplication.EndBackgroundTask(this.taskID);
				this.taskID = -1;
			}
		}

		public override void WillEnterForeground(UIApplication application)
		{
			// Called as part of the transiton from background to active state.
			// Here you can undo many of the changes made on entering the background.
		}

		public override void OnActivated(UIApplication application)
		{
			// Restart any tasks that were paused (or not yet started) while the application was inactive. 
			// If the application was previously in the background, optionally refresh the user interface.
		}

		public override void WillTerminate(UIApplication application)
		{
			// Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
		}

		/// <summary>
		/// We have to call this if our transfer (of all files!) is done.
		/// </summary>
		public static Action BackgroundSessionCompletionHandler;
	}
}

