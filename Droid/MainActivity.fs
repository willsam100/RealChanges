namespace RealEstate.Droid
open System

open Android.App
open Android.Content
open Android.Content.PM
open Android.Runtime
open Android.Views
open Android.Widget
open Android.OS
open AndroidHUD
//[<Activity (Label = "RealEstate.Droid", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
//type MainActivity() =
//    inherit Xamarin.Forms.Platform.Android.FormsApplicationActivity()
//    override this.OnCreate (bundle: Bundle) =
//        base.OnCreate (bundle)

//        Xamarin.Forms.Forms.Init (this, bundle)
//        let x = this

//        //let x =MaskType.Clear
//        let show () = 
//            AndHUD.Shared.Show(x, null, -1, MaskType.Black, System.Nullable());
//        let hide () = AndHUD.Shared.Dismiss(this)

//        this.LoadApplication (new RealEstateApp.App (
//                                                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
//                                                        new SQLitePCL.SQLite3Provider_e_sqlite3(), 
//                                                        {Show = show; Hide = hide}))
