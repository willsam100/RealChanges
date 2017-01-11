using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Android;
using Android.App;
using Android.OS;
using HockeyApp.Android;
using HockeyApp.Android.Utils;
using Xamarin.Forms;
using System.IO;

namespace RealEstate.Droid
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
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			
			var appSettings = Path.Combine(Directory.GetCurrentDirectory(), "app.config");
			
			string appId;
            if (!Directory.Exists(appSettings) || !GetSettings(appSettings).TryGetValue("hockeyapp", out appId))
            {
               appId = "";
            }


			HockeyLog.LogLevel = 1;
			HockeyApp.Android.CrashManager.Register(this, appId, new CrashManager());

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

			var info = ReCoreVerTwo.RealEstate.CreateApplication(appSettings, provider);
			LoadApplication(info.CreateApp());

		}
		
		public Dictionary<string, string> GetSettings(string path)
		{
		
		  var document = XDocument.Load(path);
		
		  var root = document.Root;
		  var results =
		    root
		      .Elements()
		      .ToDictionary(element => element.Name.ToString(), element => element.Value);
		
		  return results;
		
		}
	}
}