using Android.App;
using Android.OS;
using HockeyApp.Android;
using HockeyApp.Android.Utils;
using Xamarin.Forms;
using FFImageLoading.Forms.Droid;
using HockeyApp.Android.Metrics;
using AndroidHUD;
using System;
using Xamarinos.AdMob.Forms.Android;
using Xamarinos.AdMob.Forms;

namespace RealEstate.Droid
{
	public class CrashManagerAutoUpload : CrashManagerListener
	{
		public override bool ShouldAutoUploadCrashes() => true;
	}

	[Activity(Label = "Real Change", MainLauncher = true, NoHistory = true, Icon = "@drawable/icon", Theme = "@style/Theme.Splash")]
	public class SplashActivity : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			StartActivity(typeof(MainActivity));
		}
	}

	[Activity(Icon = "@drawable/icon", Theme = "@style/CustomActionBarTheme")]
	public class MainActivity : Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		private void IninitlizeAdMob()
		{
			AdBannerRenderer.Init();

			var adMobKey = "";
			CrossAdmobManager.Init(adMobKey);
		}


		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			CachedImageRenderer.Init();
			HockeyLog.LogLevel = 2;

			// Let's all agree to pretend this key is private :) 
			CrashManager.Register(this, "6809cf39df804d0c8c077e24edf2ea87", new CrashManagerAutoUpload());
			MetricsManager.Register(Application, "6809cf39df804d0c8c077e24edf2ea87");

			SetTheme(Resource.Style.CustomActionBarTheme);
			ActionBar.SetIcon(Android.Resource.Color.Transparent);
			//var progress = new RealEstateApp.SimpleProgres
			//{
			Action<string> show = message => AndHUD.Shared.Show(this, message, -1, MaskType.Black);
			Action hide = () => AndHUD.Shared.Dismiss();
			//	//Toast = input => AndHUD.Shared.ShowToast(this, input, timeout: TimeSpan.FromSeconds(2))
			//};

			IninitlizeAdMob();
			Action interstitial = () => CrossAdmobManager.Current.Show();

			//progress.Show();
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

			Forms.Init(this, savedInstanceState);

			var info = RealEstateCore.RealEstate.CreateApplication(path, show, hide, interstitial);
			LoadApplication(info.CreateApp());

		}
	}
}