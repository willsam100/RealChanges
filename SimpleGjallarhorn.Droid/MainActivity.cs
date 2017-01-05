using Android.App;
using Android.Widget;
using Android.OS;
using Gjallarhorn.XamarinForms;
using Xamarin.Forms;

namespace Simple.Droid
{
	[Activity(Label = "SimpleGjallarhorn.Droid", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Forms.Init(this, savedInstanceState);
			Platform.Install();


			LoadApplication(info.CreateApp());
		}
	}
}

