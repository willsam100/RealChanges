﻿using System;
using System.IO;
using FFImageLoading.Forms.Touch;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using BigTed;

namespace RealEstate.iOS
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

			CachedImageRenderer.Init();
			Corcav.Behaviors.Infrastructure.Init();

			Action<string> show = message => BTProgressHUD.Show(message);
			Action hide = BTProgressHUD.Dismiss;

			Forms.Init();
			var info = RealEstateCore.RealEstate.CreateApplication(path, show, hide);

			LoadApplication(info.CreateApp());
			return base.FinishedLaunching(uiApplication, launchOptions);
		}
	}
}

