using System;
using System.IO;
using Foundation;
using Re.Core;
using UIKit;
using Xamarin.Forms;
using Gjallarhorn.XamarinForms;
using Xamarin.Forms.Platform.iOS;

namespace RealEstate.iOSC
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
		{
			var personalFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var path = Path.Combine(personalFolder, "..", "Library");
			var provider = new SQLitePCL.SQLite3Provider_bait();
			
			Corcav.Behaviors.Infrastructure.Init();

			Forms.Init();
			var info = ReCoreVerTwo.RealEstate.CreateApplication(path, provider);

			LoadApplication(info.CreateApp());

			return base.FinishedLaunching(uiApplication, launchOptions);
		}
	}
}

