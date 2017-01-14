using Android.App;
using Android.OS;
using HockeyApp.Android;
using HockeyApp.Android.Utils;
using Xamarin.Forms;
using FFImageLoading.Forms.Droid;

namespace RealEstate.Droid
{

	public class CrashManager : CrashManagerListener
	{
		public override bool ShouldAutoUploadCrashes()
		{
			return true;
		}
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
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			CachedImageRenderer.Init();
			HockeyLog.LogLevel = 1;

			// Let's all agree to pretend this key is private :) 
			HockeyApp.Android.CrashManager.Register(this, "6809cf39df804d0c8c077e24edf2ea87", new CrashManager());

			SetTheme(Resource.Style.CustomActionBarTheme);
			ActionBar.SetIcon(Android.Resource.Color.Transparent);
			//var progress = new RealEstateApp.SimpleProgres
			//{
			//	Show = () => AndHUD.Shared.Show(this, "Loading...", -1, MaskType.Black),
			//	Hide = () => AndHUD.Shared.Dismiss(),
			//	//Toast = input => AndHUD.Shared.ShowToast(this, input, timeout: TimeSpan.FromSeconds(2))
			//};

			//progress.Show();
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

			Forms.Init(this, savedInstanceState);

			var info = RealEstateCore.RealEstate.CreateApplication(path);
			LoadApplication(info.CreateApp());

		}
	}
}