using Android.App;
using Android.Widget;
using Android.OS;
using AndroidHUD;
using HockeyApp;
using HockeyApp.Android;
using HockeyApp.Android.Utils;
using System;

namespace RealEstate.DroidC
{

	public class CrashManager : CrashManagerListener
	{
		public override bool ShouldAutoUploadCrashes()
		{
			return true;
		}
	}


	[Activity(Label = "RealEstate.DroidC", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		public const string AppID = "6809cf39df804d0c8c077e24edf2ea87";

		protected override void OnCreate(Bundle savedInstanceState)
		{

			base.OnCreate(savedInstanceState);

			HockeyLog.LogLevel = 1;

			HockeyApp.Android.CrashManager.Register(this, AppID, new CrashManager());


			ActionBar.SetIcon(Android.Resource.Color.Transparent);
			var progress = new RealEstateApp.SimpleProgres
			{
				Show = () => AndHUD.Shared.Show(this, "Loading...", -1, MaskType.Black),
				Hide = () => AndHUD.Shared.Dismiss(),
				//Toast = input => AndHUD.Shared.ShowToast(this, input, timeout: TimeSpan.FromSeconds(2))
			};

			//progress.Show();

			Xamarin.Forms.Forms.Init(this, savedInstanceState);


			Gjallarhorn.XamarinForms.Platform.Install();
			LoadApplication(new RealEstateApp.App(
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
				new SQLitePCL.SQLite3Provider_e_sqlite3(),
				progress));

		}
	}
}