using Android.App;
using Android.OS;
using Xamarin.Forms;
using AndroidHUD;
using System;

namespace RealEstate.Droid
{
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

			SetTheme(Resource.Style.CustomActionBarTheme);
			ActionBar.SetIcon(Android.Resource.Color.Transparent);
			Action<string> show = message => AndHUD.Shared.Show(this, message, -1, MaskType.Black);
			Action hide = () => AndHUD.Shared.Dismiss();

			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

			Forms.Init(this, savedInstanceState);

			var info = RealEstateCore.RealEstate.CreateApplication(path, show, hide);
			LoadApplication(info.CreateApp());
		}
	}
}