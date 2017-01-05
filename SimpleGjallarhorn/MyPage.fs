namespace SimpleGjallarhorn

open System
open Xamarin.Forms
open Gjallarhorn
open Gjallarhorn.Bindable
open Xamarin.Forms.Xaml


type ListingItem = {
  ListingId: int
  Price: string
  Title: string
  DateAdded: DateTime
  Views: int
  Image: Uri option
}

type PageTwo() = 
    inherit Xamarin.Forms.ContentPage()

    let label = Label(Text = "Boom")
    
    do
        base.Content <- label

type SimplePage() as this = 
    inherit Xamarin.Forms.ContentPage()
    
    do
        this.LoadFromXaml(typeof<SimplePage>) |> ignore

module SimpleApp = 
    let initModel i : ListingItem = {ListingId = 12; Price = "Cheap"; Title = "Signal listing"; DateAdded = DateTime.Now; Views =0; Image = None }

    type  Msg =  | NextPage // of Xamarin.Forms.ContentPage


    let runTask task = 
        do 
          async {
            do! task
                |> Async.AwaitIAsyncResult 
                |> Async.Ignore
          } |> Async.StartImmediate

    // Create a function that updates the model given a message
    let update (nav: NavigationPage) msg (model : ListingItem) =
        match msg with
        | NextPage -> (PageTwo ()) |> nav.PushAsync |> runTask; model

    let createCustomCell source (model : ISignal<ListingItem>) = 
        //let source = Binding.createSource ()

        let changed x:string = match x with 
                                | true -> "*"
                                | false ->""

        model |> Signal.map (fun v -> v.Title) |> Binding.toView source "Title"

        // Create commands for our buttons
        [
            Binding.createMessage "Next" (NextPage) source
        ]

    let applicationCore (nav: NavigationPage) = Framework.basicApplication (initModel 5) (update nav) createCustomCell 



//type App() =
//    inherit Application() 

//    let signal = Signal.constant {ListingId = 12; Price = "Cheap"; Title = "Signal listing"; DateAdded = DateTime.Now; Views =0; Image = None }
//    //let context = SimpleApp.createCustomCell signal
//    let page = SimplePage(BindingContext = context)

//    do         
//        base.MainPage <- page
