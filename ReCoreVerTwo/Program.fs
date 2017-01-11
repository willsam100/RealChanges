module ReCoreVerTwo.RealEstate
open BackgroundTasks
open Xamarin.Forms
open System.Diagnostics
open System
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.XamarinForms
open ReCoreVerTwo

let aboutComponet source (model: ISignal<Model>) = 
    List.empty<IObservable<Msg>>

let addListing source (model: ISignal<Model>) = 
    model 
    |> Signal.map (fun x -> x.AddListingModel)
    |> Signal.map (fun x -> x |> Option.map (fun y -> y.OutputMessage) |> defaultArg <| "")
    |> (fun x -> Debug.WriteLine <| sprintf "Output: %A" x.Value; x)
    |> Binding.toView source "Output"
                            
    Signal.constant "Add Trademe Listing!" |> Binding.toView source "Title" 
    
    model 
    |> Signal.map (fun x -> x.AddListingModel 
                            |> Option.map (fun y -> if y.ItemAdded then "" else y.EntryText)
                            |> defaultArg <| "" )
    |> Binding.toView source "ListingText"
        
    let isNotLoadingContent = 
        model |> Signal.map (fun x -> match x.AddListingModel with
                                        | None -> true
                                        | Some x -> not x.IsValidatingItem )
    let isOnAddListingPage = 
        model |> Signal.map (fun x -> let cp = x.CurrentPage 
                                      cp = AddListingPage)
    let canExecute = Signal.map2 (&&) isNotLoadingContent isOnAddListingPage

    Debug.WriteLine <| sprintf "Not Loading %b, page %A " isNotLoadingContent.Value model.Value.CurrentPage
    [source |> Binding.createMessageParamChecked "TrackCommand" canExecute (fun entry -> Debug.WriteLine <| sprintf "Tracking: %A" entry; RequestAction <| AddListingMessage entry)]


let detailCellComponent source (model : ISignal<ListingDownloader.FullListing>) =         

    model |> Signal.map (fun v -> v.Listing.Title)                |> Binding.toView source "Title"
    model |> Signal.map (fun v -> v.Listing.Price)                |> Binding.toView source "Price"
    model |> Signal.map (fun v -> v.Views |> sprintf "Views: %d") |> Binding.toView source "Views"

    List.empty<IObservable<Msg>>

let listingChangesComponent source (model: ISignal<Model>) = 
    let listingId = 
        model 
        |> Signal.map (fun x -> x.ListingChanges )
        |> Signal.map (fun x -> defaultArg x (ListingDownloader.TradeMe ""))
    
    Debug.WriteLine <| sprintf "ListingId %A" listingId.Value 
    
    let listings  = 
        model
        |> Signal.map (fun x -> x.Items)
        |> Signal.map List.toSeq
        |> Signal.map (Seq.filter (fun x -> x.Listing.ListingId = listingId.Value))
        |> Signal.map (Seq.sortByDescending (fun x -> x.DateAdded))
        |> Signal.map Seq.toList
        
    listings.Value |> List.iter (fun x -> Debug.WriteLine <| sprintf "Detail: %A" x)

    listings 
    |> Signal.map (fun xs -> 
        xs |> List.toSeq |> Seq.tryHead 
        |> Option.bind (fun x -> x.Image) 
        |> Option.map (fun x -> x.AbsoluteUri)
        |> defaultArg <| "" )
    |> Binding.toView source "DetailImage"
        
    let openInBrowser () = 
        Device.OpenUri(new Uri(ListingDownloader.listingIdToLink listingId.Value))
    
    listingId 
    |> Signal.map (ListingDownloader.listingIdToString) 
    |> Signal.map (fun x -> sprintf "Changes for listing: %s" x) 
    |> Binding.toView source "Title"
    
    source |> Binding.createCommandParam "ViewListing" |> Observable.add openInBrowser

    [
     BindingCollection.toView source "Details" listings detailCellComponent |> Observable.map (fun (msg,model) -> msg)
    ]

let listingChangesCell (canExecute: ISignal<bool>) source (model : ISignal<ListingDownloader.FullListing>) = 
                    
    let showAsUpdated: DateTime -> bool = fun d -> (d > (DateTime.Now.AddHours (-24.)))
                    
    let urlToString: Uri option -> string = fun image -> defaultArg (image |> Option.map (fun x -> x.AbsoluteUri)) ""

    model |> Signal.map (fun v -> v.Listing.Title)                 |> Binding.toView source "Title"
    model |> Signal.map (fun v -> v.Listing.Price)                 |> Binding.toView source "Price"
    model |> Signal.map (fun v -> v.Listing.ListingId)             |> Binding.toView source "ListingId"
    model |> Signal.map (fun v -> v.Views |> sprintf "Views: %d")  |> Binding.toView source "Views"
    model |> Signal.map (fun v -> v.DateAdded |> showAsUpdated)    |> Binding.toView source "Changed"
    model |> Signal.map (fun v -> v.Image |> urlToString)          |> Binding.toView source "Image"

    let addListingMessage = 
        Msg.RequestAction 
        <| RequestAction.SetListingDetail 
            ({
                Page = fun () -> new ListingChanges() :> Page
                Navigation = fun (navPage: INavigation) -> navPage.PushAsync
                Binding = listingChangesComponent }, model.Value.Listing.ListingId )

    Debug.WriteLine <| sprintf "Updating cell: %s %s" model.Value.Listing.Title model.Value.Listing.Price
    [
     source |> Binding.createMessageChecked "TapCommand" canExecute addListingMessage
     source |> Binding.createMessageChecked "OnDelete" canExecute (RequestAction <| DeleteListing model.Value.Listing.ListingId)
    ]
        
let listingsComponent source (model : ISignal<Model>) =    

    Debug.WriteLine <| sprintf "listViewComponent udpated: %A" model
    let sortedListings = 
        model
        |> Signal.map (fun m -> (m.Items, m.ShowRemovedListings))
        |> Signal.map (fun (listings, showRemovedListings) -> 
            listings 
            |> List.toSeq
            |> Seq.groupBy (fun x -> x.Listing.ListingId)
            |> Seq.map (fun (x, xs) -> xs |> Seq.sortBy (fun y -> y.DateAdded) |> Seq.last)
            |> Seq.filter (fun x -> x.Listing.Price <> "Sold" || showRemovedListings)
            |> Seq.filter (fun x -> x.IsActive = true || showRemovedListings)
            |> Seq.sortByDescending (fun x -> x.DateAdded)
            |> Seq.toList )

    let listingMessage = 
        Msg.ChangePage <| AddListing {
            Page = fun () -> new AddListing() :> Page
            Navigation = fun (navPage: INavigation) -> navPage.PushAsync
            Binding = addListing }
            
    let aboutMessage = 
        Msg.ChangePage <| About {
                Page = fun () -> new About() :> Page
                Navigation = fun (navPage: INavigation) -> navPage.PushAsync
                Binding = aboutComponet }
            
    let canChangePage = 
        model 
        |> Signal.map (fun x -> let cp = x.CurrentPage
                                cp = ListingsPage && (not x.IsRefreshing))
            
    sortedListings.Value |> List.iter (fun x -> Debug.WriteLine <| sprintf "Listing %s %s" x.Listing.Title x.Listing.Price)
    model |> Signal.map (fun x -> x.IsRefreshing) |> Binding.toView source "IsRefreshing"
    
    let listingChangeMessage (item: ItemTappedEventArgs) = 
        
        let model = item.Item :?> ModelBindingSource<ListingDownloader.FullListing, ReCoreVerTwo.Msg> |> (fun x -> x.Model.Value)
        Msg.RequestAction 
        <| RequestAction.SetListingDetail 
            ({
                Page = fun () -> new ListingChanges() :> Page
                Navigation = fun (navPage: INavigation) -> navPage.PushAsync
                Binding = listingChangesComponent }, model.Listing.ListingId )
                
    model 
    |> Signal.map (fun x -> if x.ShowRemovedListings then "Hide Sold" else "Show Sold")
    |> Binding.toView source "toolbarShow"

    [
        BindingCollection.toView source "Listings" sortedListings (listingChangesCell canChangePage) 
        |> Observable.map (fun (msg,model) -> msg)
        
        source |> Binding.createMessageParamChecked "ItemTapped" canChangePage listingChangeMessage
        
        source |> Binding.createMessageChecked "AddListing" canChangePage listingMessage
        source |> Binding.createMessageChecked "ShowRemoved" canChangePage (RequestAction ToggleShowRemoved)
        source |> Binding.createMessageChecked "About" canChangePage aboutMessage
        source |> Binding.createMessage "Refresh" (RequestAction RequstRefresh)
        source |> Binding.createMessage "PoppedCommand" (ChangePage Root)
    ]

let applicationRoot navPage (path, provider) = 

    let openDb path provider =  
        let db = new ApiClient()
        db.Open(path, provider) 
        db
        
    let db = openDb path provider

    let messageSource = ObservableSource<Update>()

    let load = fetchData db messageSource
    let validate = validateListing messageSource
    let save = saveItem db Debug.WriteLine
    let refresh = refreshListings messageSource
    let delete = deleteListing db messageSource
    
    let state = StateManagement(navPage, load, save, validate, refresh, delete)

    messageSource
    |> Observable.add (fun msg -> Msg.RequestCompleted msg |> state.Update)

    let app = Framework.application state.ToSignal state.Initlize state.Update listingsComponent
    app

[<CompiledName("CreateApplication")>]
let createApplication (path, provider) = 
    let page = Listings()
    let nav = new NavigationWithBehaviour(page)
    let app = applicationRoot nav (path, provider)
    Framework.createApplicationInfo app nav