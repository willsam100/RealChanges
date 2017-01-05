using Android.App;
using Android.OS;
using HockeyApp.Android;
using HockeyApp.Android.Utils;
using Gjallarhorn.XamarinForms;
using Xamarin.Forms;

namespace RealEstate.DroidC
{

	public class CrashManager : CrashManagerListener
	{
		public override bool ShouldAutoUploadCrashes()
		{
			return true;
		}
	}
	
	[Activity(Label = "RealEstate.DroidC", MainLauncher = true, NoHistory = true, Icon = "@drawable/icon", Theme = "@style/Theme.Splash")]
	public class SplashActivity : Activity
	{
	    protected override void OnCreate(Bundle savedInstanceState)
        {
           base.OnCreate(savedInstanceState);
           StartActivity(typeof(MainActivity));
		}
	}

	[Activity(Label = "RealEstate.DroidC", Icon = "@drawable/icon", Theme = "@style/CustomActionBarTheme")]
	public class MainActivity : Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		public const string AppID = "6809cf39df804d0c8c077e24edf2ea87";

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			HockeyLog.LogLevel = 1;
			HockeyApp.Android.CrashManager.Register(this, AppID, new CrashManager());
			
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
			var provider = new SQLitePCL.SQLite3Provider_e_sqlite3();

			Forms.Init(this, savedInstanceState);

			var info = ReCoreVerTwo.RealEstate.CreateApplication(path, provider);
			LoadApplication(info.CreateApp());

		}
	}
}