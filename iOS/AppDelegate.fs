namespace RealEstate.iOS

open System
open UIKit
open Foundation
open Xamarin.Forms
open Xamarin.Forms.Platform.iOS
open SQLitePCL
open Re.Core
open Gjallarhorn.XamarinForms
open System.IO

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit FormsApplicationDelegate ()
   
    override this.FinishedLaunching (app, options) =

        //let path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)

        let personalFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        let path = Path.Combine(personalFolder, "..", "Library");
        let provider = 
        
        Forms.Init()

        let page1 = new Page1();
        let app = Re.Core.RealEstate.applicationRoot(path, provider)   //ApplicationRoot(path, ());
        let info = Framework.CreateApplicationInfo(app, page1);
        this.LoadApplication(info.CreateApp());

        //this.LoadApplication (new RealEstateApp.App("", new SQLitePCL.))
        base.FinishedLaunching(app, options)

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main(args, null, "AppDelegate")
        0

